namespace PhoneBook.Application.Models;

public sealed record PhoneBookSearchRecord(
    int EntryId,
    int GroupId,
    string GroupTitle,
    string Name,
    string? Extension,
    int GroupDisplayOrder,
    int EntryDisplayOrder);
