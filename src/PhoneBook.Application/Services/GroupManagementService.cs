using PhoneBook.Application.Abstractions.Persistence;
using PhoneBook.Application.Exceptions;
using PhoneBook.Application.Models;
using PhoneBook.Domain.Entities;

namespace PhoneBook.Application.Services;

public sealed class GroupManagementService(IPhoneBookRepository repository)
{
    private const int OrderingRetryLimit = 3;

    public async Task<PhoneBookGroup> CreateGroupAsync(
        PhoneBookGroup group,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(group);

        bool automaticDisplayOrder = group.DisplayOrder <= 0;
        bool automaticPriority = group.Priority <= 0;
        for (int attempt = 1; attempt <= OrderingRetryLimit; attempt++)
        {
            PhoneBookGroup created = Copy(group);
            if (automaticDisplayOrder)
            {
                created.DisplayOrder = await repository.GetMaximumGroupDisplayOrderAsync(ct) + 1;
            }

            if (automaticPriority)
            {
                created.Priority = await repository.GetMaximumGroupPriorityAsync(ct) + 1;
            }

            try
            {
                return await repository.InsertGroupAsync(created, ct);
            }
            catch (OrderingConflictException) when (attempt < OrderingRetryLimit
                && (automaticDisplayOrder || automaticPriority))
            {
            }
        }

        throw new OrderingConflictException(
            "Could not assign a unique group order after several attempts.");
    }

    public Task UpdateGroupAsync(GroupUpdateModel group, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(group);
        GroupUpdateModel updated = group with { Title = group.Title.Trim() };
        return repository.UpdateGroupAsync(updated, ct);
    }

    public Task DeleteGroupAsync(int id, long expectedRevision, CancellationToken ct = default)
    {
        return repository.DeleteGroupAsync(id, expectedRevision, ct);
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
            IsActive = source.IsActive,
            Revision = 1
        };
    }
}
