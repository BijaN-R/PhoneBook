namespace PhoneBook.Application.Models;

public sealed record SearchHit(
    int GroupId,
    string GroupTitle,
    string Name,
    string? Extension);
