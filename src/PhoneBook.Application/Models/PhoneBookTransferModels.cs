using PhoneBook.Domain.Enums;

namespace PhoneBook.Application.Models;

public sealed record PhoneBookTransferDocument(
    int SchemaVersion,
    DateTimeOffset ExportedAtUtc,
    string Application,
    PhoneBookTransferHeader? DocumentHeader,
    PhoneBookTransferSettings? AppSettings,
    IReadOnlyList<PhoneBookTransferGroup>? Groups);

public sealed record PhoneBookTransferHeader(
    string? Title,
    string? Subtitle,
    DateOnly UpdatedAt);

public sealed record PhoneBookTransferSettings(
    double PageWidthMm,
    double PageHeightMm,
    double MarginTopMm,
    double MarginBottomMm,
    double MarginLeftMm,
    double MarginRightMm,
    double GroupGapMm,
    double CellPaddingMm,
    string? PrimaryFontFamily,
    bool UsePersianDigits,
    double MinFontSizePt,
    double DefaultFontSizePt,
    double HeaderFontSizePt,
    double GroupHeaderFontSizePt,
    int PriorityTopLimit);

public sealed record PhoneBookTransferGroup(
    string? Title,
    int Priority,
    ColumnPosition? PreferredColumn,
    int DisplayOrder,
    bool Required,
    bool KeepTogether,
    bool IsActive,
    IReadOnlyList<PhoneBookTransferEntry>? Entries);

public sealed record PhoneBookTransferEntry(
    string? Name,
    string? Extension,
    int DisplayOrder,
    bool IsActive);

public enum PhoneBookImportIssueSeverity
{
    Error,
    Warning
}

public sealed record PhoneBookImportIssue(
    PhoneBookImportIssueSeverity Severity,
    string Path,
    string Message);

public sealed record PhoneBookImportSummary(
    int GroupCount,
    int EntryCount,
    int ActiveGroupCount,
    int InactiveGroupCount,
    int ActiveEntryCount,
    int InactiveEntryCount);

public sealed record PhoneBookImportPreview(
    PhoneBookTransferDocument? Document,
    PhoneBookImportSummary Summary,
    IReadOnlyList<PhoneBookImportIssue> Issues)
{
    public bool CanApply => Document is not null
        && Issues.All(issue => issue.Severity != PhoneBookImportIssueSeverity.Error);
}

public sealed record PhoneBookImportResult(PhoneBookImportSummary Summary);

public sealed record PhoneBookExportResult(
    string FileName,
    string ContentType,
    byte[] Content);
