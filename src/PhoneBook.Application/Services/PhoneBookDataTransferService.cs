using System.Text.Json;
using PhoneBook.Application.Abstractions;
using PhoneBook.Application.Abstractions.Persistence;
using PhoneBook.Application.Models;
using PhoneBook.Core.Text;
using PhoneBook.Domain.Enums;

namespace PhoneBook.Application.Services;

public sealed class PhoneBookDataTransferService(
    IPhoneBookDataTransferRepository repository,
    IPhoneBookTransferSerializer serializer,
    IPhoneBookDataLock dataLock,
    SettingsService settingsService,
    PhoneBookSearchService searchService)
{
    public const int CurrentSchemaVersion = 1;
    public const long MaximumUploadBytes = 5 * 1024 * 1024;

    public async Task<PhoneBookExportResult> ExportAsync(CancellationToken ct = default)
    {
        PhoneBookTransferDocument document = await repository.LoadSnapshotAsync(ct);
        byte[] content = serializer.Serialize(document);
        string fileName = $"phonebook-backup-{DateTime.UtcNow:yyyy-MM-dd}.json";
        return new(fileName, "application/json", content);
    }

    public Task<PhoneBookImportPreview> ValidateImportAsync(
        ReadOnlyMemory<byte> utf8Json,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (utf8Json.Length == 0)
        {
            return Task.FromResult(InvalidDocument("فایل انتخاب‌شده خالی است."));
        }

        if (utf8Json.Length > MaximumUploadBytes)
        {
            return Task.FromResult(InvalidDocument("حجم فایل JSON نباید بیشتر از ۵ مگابایت باشد."));
        }

        try
        {
            PhoneBookTransferDocument document = serializer.Deserialize(utf8Json.Span);
            return Task.FromResult(Validate(document));
        }
        catch (JsonException)
        {
            return Task.FromResult(InvalidDocument("ساختار فایل JSON معتبر یا مطابق قالب پشتیبان نیست."));
        }
    }

    public async Task<PhoneBookImportResult> ApplyImportAsync(
        PhoneBookTransferDocument document,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        PhoneBookImportPreview preview = Validate(document);
        if (!preview.CanApply)
        {
            throw new InvalidOperationException("فایل دارای خطای اعتبارسنجی است و قابل بازیابی نیست.");
        }

        await using IAsyncDisposable lease = await dataLock.AcquireAsync(ct);
        await repository.ReplaceAsync(document, ct);
        await settingsService.ReloadAsync(ct);
        await searchService.RefreshAsync(ct);
        return new PhoneBookImportResult(preview.Summary);
    }

    private static PhoneBookImportPreview Validate(PhoneBookTransferDocument document)
    {
        List<PhoneBookImportIssue> issues = [];
        if (document.SchemaVersion != CurrentSchemaVersion)
        {
            AddError(issues, "schemaVersion", $"نسخه {document.SchemaVersion} پشتیبانی نمی‌شود؛ نسخه مورد انتظار ۱ است.");
        }

        if (document.DocumentHeader is null)
        {
            AddError(issues, "documentHeader", "بخش سربرگ الزامی است.");
        }
        else
        {
            ValidateHeader(document.DocumentHeader, issues);
        }

        if (document.AppSettings is null)
        {
            AddError(issues, "appSettings", "بخش تنظیمات الزامی است.");
        }
        else
        {
            ValidateSettings(document.AppSettings, issues);
        }

        IReadOnlyList<PhoneBookTransferGroup> groups = document.Groups ?? [];
        if (document.Groups is null)
        {
            AddError(issues, "groups", "فهرست گروه‌ها الزامی است.");
        }

        ValidateGroups(groups, issues);
        PhoneBookImportSummary summary = CreateSummary(groups);
        return new PhoneBookImportPreview(document, summary, issues);
    }

    private static void ValidateHeader(
        PhoneBookTransferHeader header,
        ICollection<PhoneBookImportIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(header.Title))
        {
            AddError(issues, "documentHeader.title", "عنوان سربرگ الزامی است.");
        }
        else if (header.Title.Length > 200)
        {
            AddError(issues, "documentHeader.title", "عنوان سربرگ حداکثر ۲۰۰ نویسه است.");
        }

        if (header.Subtitle?.Length > 500)
        {
            AddError(issues, "documentHeader.subtitle", "زیرعنوان حداکثر ۵۰۰ نویسه است.");
        }
    }

    private static void ValidateSettings(
        PhoneBookTransferSettings settings,
        ICollection<PhoneBookImportIssue> issues)
    {
        double[] positive =
        [
            settings.PageWidthMm, settings.PageHeightMm, settings.MinFontSizePt,
            settings.DefaultFontSizePt, settings.HeaderFontSizePt, settings.GroupHeaderFontSizePt
        ];
        if (positive.Any(value => !double.IsFinite(value) || value <= 0))
        {
            AddError(issues, "appSettings", "ابعاد صفحه و اندازه‌های قلم باید عدد مثبت باشند.");
        }

        double[] nonNegative =
        [
            settings.MarginTopMm, settings.MarginBottomMm, settings.MarginLeftMm,
            settings.MarginRightMm, settings.GroupGapMm, settings.CellPaddingMm
        ];
        if (nonNegative.Any(value => !double.IsFinite(value) || value < 0))
        {
            AddError(issues, "appSettings", "حاشیه‌ها و فاصله‌ها نمی‌توانند منفی باشند.");
        }

        if (settings.MarginLeftMm + settings.MarginRightMm >= settings.PageWidthMm
            || settings.MarginTopMm + settings.MarginBottomMm >= settings.PageHeightMm)
        {
            AddError(issues, "appSettings", "مجموع حاشیه‌ها باید از ابعاد صفحه کوچک‌تر باشد.");
        }

        if (settings.DefaultFontSizePt < settings.MinFontSizePt
            || settings.HeaderFontSizePt < settings.MinFontSizePt
            || settings.GroupHeaderFontSizePt < settings.MinFontSizePt)
        {
            AddError(issues, "appSettings", "اندازه قلم‌ها نمی‌تواند از کمینه قلم کوچک‌تر باشد.");
        }

        if (string.IsNullOrWhiteSpace(settings.PrimaryFontFamily))
        {
            AddError(issues, "appSettings.primaryFontFamily", "نام خانواده قلم الزامی است.");
        }
        else if (settings.PrimaryFontFamily.Length > 100)
        {
            AddError(issues, "appSettings.primaryFontFamily", "نام خانواده قلم حداکثر ۱۰۰ نویسه است.");
        }

        if (settings.PriorityTopLimit != 3)
        {
            AddError(issues, "appSettings.priorityTopLimit", "تعداد اولویت‌های بالا باید ۳ باشد.");
        }
    }

    private static void ValidateGroups(
        IReadOnlyList<PhoneBookTransferGroup> groups,
        ICollection<PhoneBookImportIssue> issues)
    {
        AddDuplicateErrors(groups.Select(group => group.DisplayOrder), "groups.displayOrder", "ترتیب نمایش گروه‌ها باید یکتا باشد.", issues);
        AddDuplicateErrors(groups.Select(group => group.Priority), "groups.priority", "اولویت گروه‌ها باید یکتا باشد.", issues);

        Dictionary<string, List<string>> extensionPaths = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, List<string>> personPaths = new(StringComparer.Ordinal);
        for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
        {
            PhoneBookTransferGroup group = groups[groupIndex];
            string groupPath = $"groups[{groupIndex}]";
            if (string.IsNullOrWhiteSpace(group.Title))
            {
                AddError(issues, $"{groupPath}.title", "عنوان گروه الزامی است.");
            }
            else if (group.Title.Length > 150)
            {
                AddError(issues, $"{groupPath}.title", "عنوان گروه حداکثر ۱۵۰ نویسه است.");
            }

            if (group.Priority <= 0)
            {
                AddError(issues, $"{groupPath}.priority", "اولویت گروه باید بزرگ‌تر از صفر باشد.");
            }
            if (group.DisplayOrder <= 0)
            {
                AddError(issues, $"{groupPath}.displayOrder", "ترتیب گروه باید بزرگ‌تر از صفر باشد.");
            }
            if (group.PreferredColumn is not null && !Enum.IsDefined(group.PreferredColumn.Value))
            {
                AddError(issues, $"{groupPath}.preferredColumn", "ستون پیشنهادی معتبر نیست.");
            }

            IReadOnlyList<PhoneBookTransferEntry> entries = group.Entries ?? [];
            if (group.Entries is null)
            {
                AddError(issues, $"{groupPath}.entries", "فهرست داخلی‌های گروه الزامی است.");
            }
            AddDuplicateErrors(entries.Select(entry => entry.DisplayOrder), $"{groupPath}.entries.displayOrder", "ترتیب داخلی‌ها در هر گروه باید یکتا باشد.", issues);

            for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
            {
                PhoneBookTransferEntry entry = entries[entryIndex];
                string entryPath = $"{groupPath}.entries[{entryIndex}]";
                if (string.IsNullOrWhiteSpace(entry.Name) && string.IsNullOrWhiteSpace(entry.Extension))
                {
                    AddError(issues, entryPath, "هر داخلی باید نام یا شماره داشته باشد.");
                }
                if (entry.Name?.Length > 200)
                {
                    AddError(issues, $"{entryPath}.name", "نام حداکثر ۲۰۰ نویسه است.");
                }
                if (entry.Extension?.Length > 50)
                {
                    AddError(issues, $"{entryPath}.extension", "شماره داخلی حداکثر ۵۰ نویسه است.");
                }
                if (entry.DisplayOrder <= 0)
                {
                    AddError(issues, $"{entryPath}.displayOrder", "ترتیب داخلی باید بزرگ‌تر از صفر باشد.");
                }

                string extension = entry.Extension?.Trim() ?? string.Empty;
                if (extension.Length > 0)
                {
                    extensionPaths.GetOrAdd(extension).Add(entryPath);
                }
                string personKey = $"{PersianTextNormalizer.NormalizeForSearch(entry.Name ?? string.Empty)}\u001f{PersianTextNormalizer.NormalizeForSearch(extension)}";
                if (personKey != "\u001f")
                {
                    personPaths.GetOrAdd(personKey).Add(entryPath);
                }
            }
        }

        AddDuplicateWarnings(extensionPaths, "شماره داخلی تکراری", issues);
        AddDuplicateWarnings(personPaths, "نام و شماره یکسان", issues);
    }

    private static PhoneBookImportSummary CreateSummary(IReadOnlyList<PhoneBookTransferGroup> groups)
    {
        PhoneBookTransferEntry[] entries = groups.SelectMany(group => group.Entries ?? []).ToArray();
        return new(
            groups.Count,
            entries.Length,
            groups.Count(group => group.IsActive),
            groups.Count(group => !group.IsActive),
            entries.Count(entry => entry.IsActive),
            entries.Count(entry => !entry.IsActive));
    }

    private static void AddDuplicateErrors(
        IEnumerable<int> values,
        string path,
        string message,
        ICollection<PhoneBookImportIssue> issues)
    {
        if (values.GroupBy(value => value).Any(group => group.Count() > 1))
        {
            AddError(issues, path, message);
        }
    }

    private static void AddDuplicateWarnings(
        IReadOnlyDictionary<string, List<string>> values,
        string label,
        ICollection<PhoneBookImportIssue> issues)
    {
        foreach ((string value, List<string> paths) in values.Where(item => item.Value.Count > 1))
        {
            issues.Add(new(
                PhoneBookImportIssueSeverity.Warning,
                string.Join(", ", paths),
                $"{label}: «{value}»"));
        }
    }

    private static PhoneBookImportPreview InvalidDocument(string message)
    {
        return new(
            null,
            new PhoneBookImportSummary(0, 0, 0, 0, 0, 0),
            [new PhoneBookImportIssue(PhoneBookImportIssueSeverity.Error, "$", message)]);
    }

    private static void AddError(
        ICollection<PhoneBookImportIssue> issues,
        string path,
        string message)
    {
        issues.Add(new(PhoneBookImportIssueSeverity.Error, path, message));
    }
}

file static class DictionaryExtensions
{
    public static List<string> GetOrAdd(
        this Dictionary<string, List<string>> dictionary,
        string key)
    {
        if (!dictionary.TryGetValue(key, out List<string>? value))
        {
            value = [];
            dictionary.Add(key, value);
        }
        return value;
    }
}
