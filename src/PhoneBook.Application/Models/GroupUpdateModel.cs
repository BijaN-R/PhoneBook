using PhoneBook.Domain.Entities;
using PhoneBook.Domain.Enums;

namespace PhoneBook.Application.Models;

public sealed record GroupUpdateModel(
    int Id,
    long ExpectedRevision,
    string Title,
    int Priority,
    ColumnPosition? PreferredColumn,
    int DisplayOrder,
    bool Required,
    bool KeepTogether,
    bool IsActive)
{
    public static GroupUpdateModel FromEntity(PhoneBookGroup group)
    {
        ArgumentNullException.ThrowIfNull(group);
        return new(
            group.Id,
            group.Revision,
            group.Title,
            group.Priority,
            group.PreferredColumn,
            group.DisplayOrder,
            group.Required,
            group.KeepTogether,
            group.IsActive);
    }
}
