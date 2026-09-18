namespace PhoneBook.Application.Models;

public enum SearchMatchKind
{
    None,
    FuzzyTwoEdits,
    FuzzyOneEdit,
    Substring,
    Prefix,
    ExactGroupToken,
    ExactFullGroupTitle,
    ExactNameToken,
    ExactFullName,
    ExactExtension
}

public sealed record SearchHit(
    int EntryId,
    int GroupId,
    string GroupTitle,
    string Name,
    string? Extension,
    int GroupDisplayOrder,
    int EntryDisplayOrder,
    int Score,
    SearchMatchKind MatchKind);
