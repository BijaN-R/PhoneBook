using PhoneBook.Application.Abstractions.Persistence;
using PhoneBook.Application.Abstractions;
using PhoneBook.Application.Models;
using PhoneBook.Core.Text;
using PhoneBook.Domain.Entities;

namespace PhoneBook.Application.Services;

public sealed class PhoneBookDataQualityService(IPhoneBookRepository repository) : IPhoneBookDataQualityService
{
    public async Task<DataQualityReport> AnalyzeAsync(CancellationToken ct = default)
    {
        IReadOnlyList<PhoneBookGroup> groups = await repository.GetGroupsAsync(false, ct);
        List<DataQualityIssue> issues = [];

        AddOrderingIssues(groups, issues);
        Dictionary<string, List<(PhoneBookGroup Group, PhoneBookEntry Entry)>> extensions = new(StringComparer.Ordinal);
        foreach (PhoneBookGroup group in groups)
        {
            PhoneBookEntry[] entries = group.Entries.ToArray();
            foreach (PhoneBookEntry entry in entries)
            {
                string name = PersianTextNormalizer.NormalizeForSearch(entry.Name ?? string.Empty);
                string extension = PersianTextNormalizer.NormalizeForSearch(entry.Extension ?? string.Empty);
                if (name.Length == 0 && extension.Length == 0)
                    issues.Add(new(DataQualitySeverity.Error, DataQualityCategory.EmptyEntry, group.Id, entry.Id, "نام و شماره داخلی هر دو خالی هستند."));
                if (extension.Length > 0)
                {
                    if (!extensions.TryGetValue(extension, out var values)) extensions[extension] = values = [];
                    values.Add((group, entry));
                }
            }

            foreach (var duplicate in entries.GroupBy(entry => new
            {
                Name = PersianTextNormalizer.NormalizeForSearch(entry.Name ?? string.Empty),
                Extension = PersianTextNormalizer.NormalizeForSearch(entry.Extension ?? string.Empty)
            }).Where(item => item.Count() > 1 && (item.Key.Name.Length > 0 || item.Key.Extension.Length > 0)))
            {
                foreach (PhoneBookEntry entry in duplicate)
                    issues.Add(new(DataQualitySeverity.Warning, DataQualityCategory.ExactDuplicatePerson, group.Id, entry.Id, "نام و شماره یکسان در همین گروه تکرار شده است."));
            }

            for (int first = 0; first < entries.Length; first++)
            for (int second = first + 1; second < entries.Length; second++)
            {
                string a = PersianTextNormalizer.NormalizeForSearch(entries[first].Name ?? string.Empty);
                string b = PersianTextNormalizer.NormalizeForSearch(entries[second].Name ?? string.Empty);
                if (a != b && a.Length >= 4 && b.Length >= 4
                    && PersianFuzzyMatcher.OptimalStringAlignmentDistance(a, b) == 1)
                {
                    issues.Add(new(DataQualitySeverity.Warning, DataQualityCategory.NearDuplicateName,
                        group.Id, entries[second].Id, $"نام‌های «{entries[first].Name}» و «{entries[second].Name}» بسیار شبیه‌اند."));
                }
            }
        }

        foreach (var repeated in extensions.Where(item => item.Value.Count > 1))
        foreach (var value in repeated.Value)
            issues.Add(new(DataQualitySeverity.Warning, DataQualityCategory.RepeatedExtension,
                value.Group.Id, value.Entry.Id, $"شماره داخلی «{repeated.Key}» در چند رکورد استفاده شده است."));

        return new(issues.OrderByDescending(issue => issue.Severity).ThenBy(issue => issue.GroupId).ThenBy(issue => issue.EntryId).ToArray());
    }

    private static void AddOrderingIssues(IReadOnlyList<PhoneBookGroup> groups, ICollection<DataQualityIssue> issues)
    {
        foreach (PhoneBookGroup group in groups)
        {
            if (group.DisplayOrder <= 0 || group.Priority <= 0)
                issues.Add(new(DataQualitySeverity.Error, DataQualityCategory.InvalidOrdering, group.Id, null, "ترتیب نمایش یا اولویت گروه باید بزرگ‌تر از صفر باشد."));
            foreach (PhoneBookEntry entry in group.Entries.Where(entry => entry.DisplayOrder <= 0))
                issues.Add(new(DataQualitySeverity.Error, DataQualityCategory.InvalidOrdering, group.Id, entry.Id, "ترتیب نمایش داخلی باید بزرگ‌تر از صفر باشد."));
            foreach (var duplicate in group.Entries.GroupBy(entry => entry.DisplayOrder).Where(item => item.Count() > 1))
                issues.Add(new(DataQualitySeverity.Error, DataQualityCategory.DuplicateOrdering, group.Id, null, $"ترتیب داخلی {duplicate.Key} در این گروه تکراری است."));
        }
        foreach (var duplicate in groups.GroupBy(group => group.DisplayOrder).Where(item => item.Count() > 1))
            issues.Add(new(DataQualitySeverity.Error, DataQualityCategory.DuplicateOrdering, null, null, $"ترتیب نمایش گروه {duplicate.Key} تکراری است."));
        foreach (var duplicate in groups.GroupBy(group => group.Priority).Where(item => item.Count() > 1))
            issues.Add(new(DataQualitySeverity.Error, DataQualityCategory.DuplicateOrdering, null, null, $"اولویت گروه {duplicate.Key} تکراری است."));
    }
}
