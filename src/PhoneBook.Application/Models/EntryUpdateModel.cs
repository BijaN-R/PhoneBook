using PhoneBook.Domain.Entities;

namespace PhoneBook.Application.Models;

public sealed record EntryUpdateModel(
    int Id,
    long ExpectedRevision,
    string Name,
    string? Extension,
    int DisplayOrder,
    bool IsActive)
{
    public static EntryUpdateModel FromEntity(PhoneBookEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return new(
            entry.Id,
            entry.Revision,
            entry.Name,
            entry.Extension,
            entry.DisplayOrder,
            entry.IsActive);
    }
}
