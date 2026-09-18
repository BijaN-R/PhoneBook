using PhoneBook.Application.Abstractions.Persistence;
using PhoneBook.Domain.Entities;

namespace PhoneBook.Application.Services;

public sealed class GroupManagementService(IPhoneBookRepository repository)
{
    public async Task<PhoneBookGroup> CreateGroupAsync(
        PhoneBookGroup group,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(group);

        PhoneBookGroup created = Copy(group);
        if (created.DisplayOrder <= 0)
        {
            created.DisplayOrder = await repository.GetMaximumGroupDisplayOrderAsync(ct) + 1;
        }

        if (created.Priority <= 0)
        {
            created.Priority = await repository.GetMaximumGroupPriorityAsync(ct) + 1;
        }

        return await repository.InsertGroupAsync(created, ct);
    }

    public async Task UpdateGroupAsync(PhoneBookGroup group, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(group);

        PhoneBookGroup updated = Copy(group);
        updated.Id = group.Id;
        if (updated.DisplayOrder <= 0)
        {
            updated.DisplayOrder = await repository.GetMaximumGroupDisplayOrderAsync(ct) + 1;
        }

        if (updated.Priority <= 0)
        {
            updated.Priority = await repository.GetMaximumGroupPriorityAsync(ct) + 1;
        }

        await repository.UpdateGroupAsync(updated, ct);
    }

    public Task DeleteGroupAsync(int id, CancellationToken ct = default)
    {
        return repository.DeleteGroupAsync(id, ct);
    }

    private static PhoneBookGroup Copy(PhoneBookGroup source)
    {
        return new PhoneBookGroup
        {
            Title = source.Title.Trim(),
            Priority = source.Priority,
            PreferredColumn = source.PreferredColumn,
            DisplayOrder = source.DisplayOrder,
            Required = source.Required,
            KeepTogether = source.KeepTogether,
            IsActive = source.IsActive
        };
    }
}
