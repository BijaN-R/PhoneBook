using PhoneBook.Application.Abstractions.Persistence;
using PhoneBook.Domain.Entities;

namespace PhoneBook.Application.Services;

public sealed class EntryManagementService(IPhoneBookRepository repository)
{
    public async Task<PhoneBookEntry> CreateEntryAsync(
        PhoneBookEntry entry,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (await repository.GetGroupAsync(entry.GroupId, ct) is null)
        {
            throw new KeyNotFoundException($"Group {entry.GroupId} was not found.");
        }

        PhoneBookEntry created = Copy(entry);
        if (created.DisplayOrder <= 0)
        {
            created.DisplayOrder = await repository.GetMaximumEntryDisplayOrderAsync(
                created.GroupId,
                ct) + 1;
        }

        return await repository.InsertEntryAsync(created, ct);
    }

    public Task UpdateEntryAsync(PhoneBookEntry entry, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        PhoneBookEntry updated = Copy(entry);
        updated.Id = entry.Id;
        return repository.UpdateEntryAsync(updated, ct);
    }

    public Task DeleteEntryAsync(int id, CancellationToken ct = default)
    {
        return repository.DeleteEntryAsync(id, ct);
    }

    public async Task MoveEntryAsync(int entryId, int offset, CancellationToken ct = default)
    {
        if (offset is not (-1 or 1))
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Offset must be -1 or 1.");
        }

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

        await repository.SwapEntryDisplayOrdersAsync(entryId, ordered[targetIndex].Id, ct);
    }

    private static PhoneBookEntry Copy(PhoneBookEntry source)
    {
        return new PhoneBookEntry
        {
            GroupId = source.GroupId,
            Name = source.Name.Trim(),
            Extension = string.IsNullOrWhiteSpace(source.Extension) ? null : source.Extension.Trim(),
            DisplayOrder = source.DisplayOrder,
            IsActive = source.IsActive
        };
    }
}
