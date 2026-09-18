using PhoneBook.Application.Abstractions;
using PhoneBook.Application.Abstractions.Persistence;
using PhoneBook.Application.Exceptions;
using PhoneBook.Application.Models;
using PhoneBook.Domain.Entities;

namespace PhoneBook.Application.Services;

public sealed class EntryManagementService(
    IPhoneBookRepository repository,
    IPhoneBookDataLock dataLock)
{
    private const int OrderingRetryLimit = 3;

    public async Task<PhoneBookEntry> CreateEntryAsync(
        PhoneBookEntry entry,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        await using IAsyncDisposable lease = await dataLock.AcquireAsync(ct);

        if (await repository.GetGroupAsync(entry.GroupId, ct) is null)
        {
            throw new KeyNotFoundException($"Group {entry.GroupId} was not found.");
        }

        bool automaticDisplayOrder = entry.DisplayOrder <= 0;
        for (int attempt = 1; attempt <= OrderingRetryLimit; attempt++)
        {
            PhoneBookEntry created = Copy(entry);
            if (automaticDisplayOrder)
            {
                created.DisplayOrder = await repository.GetMaximumEntryDisplayOrderAsync(
                    created.GroupId,
                    ct) + 1;
            }

            try
            {
                return await repository.InsertEntryAsync(created, ct);
            }
            catch (OrderingConflictException) when (attempt < OrderingRetryLimit
                && automaticDisplayOrder)
            {
            }
        }

        throw new OrderingConflictException(
            "Could not assign a unique entry order after several attempts.");
    }

    public async Task UpdateEntryAsync(EntryUpdateModel entry, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        await using IAsyncDisposable lease = await dataLock.AcquireAsync(ct);
        EntryUpdateModel updated = entry with
        {
            Name = entry.Name.Trim(),
            Extension = string.IsNullOrWhiteSpace(entry.Extension) ? null : entry.Extension.Trim()
        };
        await repository.UpdateEntryAsync(updated, ct);
    }

    public async Task DeleteEntryAsync(int id, long expectedRevision, CancellationToken ct = default)
    {
        await using IAsyncDisposable lease = await dataLock.AcquireAsync(ct);
        await repository.DeleteEntryAsync(id, expectedRevision, ct);
    }

    public async Task MoveEntryAsync(
        int entryId,
        long expectedRevision,
        int offset,
        CancellationToken ct = default)
    {
        if (offset is not (-1 or 1))
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Offset must be -1 or 1.");
        }

        await using IAsyncDisposable lease = await dataLock.AcquireAsync(ct);

        PhoneBookEntry moving = await repository.GetEntryAsync(entryId, ct)
            ?? throw new KeyNotFoundException($"Entry {entryId} was not found.");
        IReadOnlyList<PhoneBookEntry> ordered = await repository.GetEntriesAsync(
            moving.GroupId,
            activeOnly: false,
            ct);
        int currentIndex = ordered.ToList().FindIndex(item => item.Id == entryId);
        int targetIndex = currentIndex + offset;
        if (currentIndex < 0 || targetIndex < 0 || targetIndex >= ordered.Count)
        {
            return;
        }

        PhoneBookEntry neighbor = ordered[targetIndex];
        await repository.SwapEntryDisplayOrdersAsync(
            entryId,
            expectedRevision,
            neighbor.Id,
            neighbor.Revision,
            ct);
    }

    private static PhoneBookEntry Copy(PhoneBookEntry source)
    {
        return new PhoneBookEntry
        {
            GroupId = source.GroupId,
            Name = source.Name.Trim(),
            Extension = string.IsNullOrWhiteSpace(source.Extension) ? null : source.Extension.Trim(),
            DisplayOrder = source.DisplayOrder,
            IsActive = source.IsActive,
            Revision = 1
        };
    }
}
