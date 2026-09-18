namespace PhoneBook.Application.Models;

public sealed record PhoneBookSearchRecord(
    int GroupId,
    string GroupTitle,
    string Name,
    string? Extension);
