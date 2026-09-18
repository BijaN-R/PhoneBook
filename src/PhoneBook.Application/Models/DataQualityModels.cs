namespace PhoneBook.Application.Models;

public enum DataQualitySeverity
{
    Warning,
    Error
}

public enum DataQualityCategory
{
    EmptyEntry,
    ExactDuplicatePerson,
    RepeatedExtension,
    NearDuplicateName,
    InvalidOrdering,
    DuplicateOrdering
}

public sealed record DataQualityIssue(
    DataQualitySeverity Severity,
    DataQualityCategory Category,
    int? GroupId,
    int? EntryId,
    string Message);

public sealed record DataQualityReport(IReadOnlyList<DataQualityIssue> Issues);
