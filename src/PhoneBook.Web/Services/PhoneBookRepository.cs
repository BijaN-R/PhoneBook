// FILE: src/PhoneBook.Web/Services/PhoneBookRepository.cs
using Microsoft.EntityFrameworkCore;
using PhoneBook.Domain.Entities;
using PhoneBook.Infrastructure.Data;

namespace PhoneBook.Web.Services;

public sealed class PhoneBookRepository(IDbContextFactory<AppDbContext> contextFactory)
{
    public async Task<DocumentHeader> GetDocumentHeaderAsync(CancellationToken ct = default)
    {
        await using AppDbContext context = await contextFactory.CreateDbContextAsync(ct);
        return await context.DocumentHeaders.AsNoTracking().SingleOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("The document header row is missing from the database.");
    }

    public async Task<IReadOnlyList<PhoneBookGroup>> GetGroupsAsync(
        bool activeOnly = false,
        CancellationToken ct = default)
    {
        await using AppDbContext context = await contextFactory.CreateDbContextAsync(ct);
        IQueryable<PhoneBookGroup> query = context.PhoneBookGroups
            .AsNoTracking()
            .Include(group => group.Entries);

        if (activeOnly)
        {
            query = query.Where(group => group.IsActive);
        }

        List<PhoneBookGroup> groups = await query
            .OrderBy(group => group.DisplayOrder)
            .ThenBy(group => group.Id)
            .ToListAsync(ct);

        foreach (PhoneBookGroup group in groups)
        {
            group.Entries = group.Entries
                .Where(entry => !activeOnly || entry.IsActive)
                .OrderBy(entry => entry.DisplayOrder)
                .ThenBy(entry => entry.Id)
                .ToList();
        }

        return groups;
    }

    public async Task<PhoneBookGroup?> GetGroupAsync(int id, CancellationToken ct = default)
    {
        await using AppDbContext context = await contextFactory.CreateDbContextAsync(ct);
        return await context.PhoneBookGroups
            .AsNoTracking()
            .Include(group => group.Entries.OrderBy(entry => entry.DisplayOrder))
            .SingleOrDefaultAsync(group => group.Id == id, ct);
    }

    public async Task<PhoneBookGroup> AddGroupAsync(
        PhoneBookGroup group,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(group);

        await using AppDbContext context = await contextFactory.CreateDbContextAsync(ct);
        PhoneBookGroup created = CopyGroup(group);
        if (created.DisplayOrder <= 0)
        {
            created.DisplayOrder = (await context.PhoneBookGroups.MaxAsync(
                item => (int?)item.DisplayOrder,
                ct) ?? 0) + 1;
        }

        if (created.Priority <= 0)
        {
            created.Priority = (await context.PhoneBookGroups.MaxAsync(
                item => (int?)item.Priority,
                ct) ?? 0) + 1;
        }

        context.PhoneBookGroups.Add(created);
        await context.SaveChangesAsync(ct);
        return created;
    }

    public async Task UpdateGroupAsync(PhoneBookGroup group, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(group);

        await using AppDbContext context = await contextFactory.CreateDbContextAsync(ct);
        PhoneBookGroup existing = await context.PhoneBookGroups.SingleOrDefaultAsync(
            item => item.Id == group.Id,
            ct) ?? throw new KeyNotFoundException($"Group {group.Id} was not found.");

        existing.Title = group.Title.Trim();
        existing.Priority = group.Priority;
        existing.PreferredColumn = group.PreferredColumn;
        existing.DisplayOrder = group.DisplayOrder;
        existing.Required = group.Required;
        existing.KeepTogether = group.KeepTogether;
        existing.IsActive = group.IsActive;
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteGroupAsync(int id, CancellationToken ct = default)
    {
        await using AppDbContext context = await contextFactory.CreateDbContextAsync(ct);
        PhoneBookGroup? group = await context.PhoneBookGroups.SingleOrDefaultAsync(
            item => item.Id == id,
            ct);
        if (group is null)
        {
            return;
        }

        context.PhoneBookGroups.Remove(group);
        await context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<PhoneBookEntry>> GetEntriesAsync(
        int groupId,
        bool activeOnly = false,
        CancellationToken ct = default)
    {
        await using AppDbContext context = await contextFactory.CreateDbContextAsync(ct);
        IQueryable<PhoneBookEntry> query = context.PhoneBookEntries
            .AsNoTracking()
            .Where(entry => entry.GroupId == groupId);
        if (activeOnly)
        {
            query = query.Where(entry => entry.IsActive);
        }

        return await query
            .OrderBy(entry => entry.DisplayOrder)
            .ThenBy(entry => entry.Id)
            .ToListAsync(ct);
    }

    public async Task<PhoneBookEntry> AddEntryAsync(
        PhoneBookEntry entry,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        await using AppDbContext context = await contextFactory.CreateDbContextAsync(ct);
        bool groupExists = await context.PhoneBookGroups.AnyAsync(
            group => group.Id == entry.GroupId,
            ct);
        if (!groupExists)
        {
            throw new KeyNotFoundException($"Group {entry.GroupId} was not found.");
        }

        PhoneBookEntry created = CopyEntry(entry);
        if (created.DisplayOrder <= 0)
        {
            created.DisplayOrder = (await context.PhoneBookEntries
                .Where(item => item.GroupId == created.GroupId)
                .MaxAsync(item => (int?)item.DisplayOrder, ct) ?? 0) + 1;
        }

        context.PhoneBookEntries.Add(created);
        await context.SaveChangesAsync(ct);
        return created;
    }

    public async Task UpdateEntryAsync(PhoneBookEntry entry, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        await using AppDbContext context = await contextFactory.CreateDbContextAsync(ct);
        PhoneBookEntry existing = await context.PhoneBookEntries.SingleOrDefaultAsync(
            item => item.Id == entry.Id,
            ct) ?? throw new KeyNotFoundException($"Entry {entry.Id} was not found.");

        existing.Name = entry.Name.Trim();
        existing.Extension = string.IsNullOrWhiteSpace(entry.Extension) ? null : entry.Extension.Trim();
        existing.DisplayOrder = entry.DisplayOrder;
        existing.IsActive = entry.IsActive;
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteEntryAsync(int id, CancellationToken ct = default)
    {
        await using AppDbContext context = await contextFactory.CreateDbContextAsync(ct);
        PhoneBookEntry? entry = await context.PhoneBookEntries.SingleOrDefaultAsync(
            item => item.Id == id,
            ct);
        if (entry is null)
        {
            return;
        }

        context.PhoneBookEntries.Remove(entry);
        await context.SaveChangesAsync(ct);
    }

    public async Task MoveEntryAsync(int entryId, int offset, CancellationToken ct = default)
    {
        if (offset is not (-1 or 1))
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Offset must be -1 or 1.");
        }

        await using AppDbContext context = await contextFactory.CreateDbContextAsync(ct);
        PhoneBookEntry moving = await context.PhoneBookEntries.SingleOrDefaultAsync(
            item => item.Id == entryId,
            ct) ?? throw new KeyNotFoundException($"Entry {entryId} was not found.");
        List<PhoneBookEntry> ordered = await context.PhoneBookEntries
            .Where(item => item.GroupId == moving.GroupId)
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.Id)
            .ToListAsync(ct);
        int currentIndex = ordered.FindIndex(item => item.Id == entryId);
        int targetIndex = currentIndex + offset;
        if (currentIndex < 0 || targetIndex < 0 || targetIndex >= ordered.Count)
        {
            return;
        }

        PhoneBookEntry neighbor = ordered[targetIndex];
        int movingOrder = moving.DisplayOrder;
        int neighborOrder = neighbor.DisplayOrder;
        int temporaryOrder = ordered.Max(item => item.DisplayOrder) + 1;

        await using var transaction = await context.Database.BeginTransactionAsync(ct);
        moving.DisplayOrder = temporaryOrder;
        await context.SaveChangesAsync(ct);
        neighbor.DisplayOrder = movingOrder;
        await context.SaveChangesAsync(ct);
        moving.DisplayOrder = neighborOrder;
        await context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    private static PhoneBookGroup CopyGroup(PhoneBookGroup source)
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

    private static PhoneBookEntry CopyEntry(PhoneBookEntry source)
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
