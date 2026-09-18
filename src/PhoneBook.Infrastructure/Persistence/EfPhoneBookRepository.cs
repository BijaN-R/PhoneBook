using Microsoft.EntityFrameworkCore;
using PhoneBook.Application.Abstractions.Persistence;
using PhoneBook.Application.Models;
using PhoneBook.Domain.Entities;
using PhoneBook.Infrastructure.Data;

namespace PhoneBook.Infrastructure.Persistence;

public sealed class EfPhoneBookRepository(IDbContextFactory<AppDbContext> contextFactory)
    : IPhoneBookRepository
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

    public async Task<PhoneBookEntry?> GetEntryAsync(int id, CancellationToken ct = default)
    {
        await using AppDbContext context = await contextFactory.CreateDbContextAsync(ct);
        return await context.PhoneBookEntries
            .AsNoTracking()
            .SingleOrDefaultAsync(entry => entry.Id == id, ct);
    }

    public async Task<int> GetMaximumGroupDisplayOrderAsync(CancellationToken ct = default)
    {
        await using AppDbContext context = await contextFactory.CreateDbContextAsync(ct);
        return await context.PhoneBookGroups.MaxAsync(group => (int?)group.DisplayOrder, ct) ?? 0;
    }

    public async Task<int> GetMaximumGroupPriorityAsync(CancellationToken ct = default)
    {
        await using AppDbContext context = await contextFactory.CreateDbContextAsync(ct);
        return await context.PhoneBookGroups.MaxAsync(group => (int?)group.Priority, ct) ?? 0;
    }

    public async Task<int> GetMaximumEntryDisplayOrderAsync(
        int groupId,
        CancellationToken ct = default)
    {
        await using AppDbContext context = await contextFactory.CreateDbContextAsync(ct);
        return await context.PhoneBookEntries
            .Where(entry => entry.GroupId == groupId)
            .MaxAsync(entry => (int?)entry.DisplayOrder, ct) ?? 0;
    }

    public async Task<PhoneBookGroup> InsertGroupAsync(
        PhoneBookGroup group,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(group);

        await using AppDbContext context = await contextFactory.CreateDbContextAsync(ct);
        PhoneBookGroup created = CopyGroup(group);
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

        existing.Title = group.Title;
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

    public async Task<PhoneBookEntry> InsertEntryAsync(
        PhoneBookEntry entry,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        await using AppDbContext context = await contextFactory.CreateDbContextAsync(ct);
        PhoneBookEntry created = CopyEntry(entry);
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

        existing.Name = entry.Name;
        existing.Extension = entry.Extension;
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

    public async Task SwapEntryDisplayOrdersAsync(
        int firstEntryId,
        int secondEntryId,
        CancellationToken ct = default)
    {
        await using AppDbContext context = await contextFactory.CreateDbContextAsync(ct);
        PhoneBookEntry first = await context.PhoneBookEntries.SingleOrDefaultAsync(
            entry => entry.Id == firstEntryId,
            ct) ?? throw new KeyNotFoundException($"Entry {firstEntryId} was not found.");
        PhoneBookEntry second = await context.PhoneBookEntries.SingleOrDefaultAsync(
            entry => entry.Id == secondEntryId,
            ct) ?? throw new KeyNotFoundException($"Entry {secondEntryId} was not found.");

        if (first.GroupId != second.GroupId)
        {
            throw new InvalidOperationException("Only entries in the same group can exchange display order.");
        }

        int firstOrder = first.DisplayOrder;
        int secondOrder = second.DisplayOrder;
        int temporaryOrder = (await context.PhoneBookEntries
            .Where(entry => entry.GroupId == first.GroupId)
            .MaxAsync(entry => (int?)entry.DisplayOrder, ct) ?? 0) + 1;

        await using var transaction = await context.Database.BeginTransactionAsync(ct);
        first.DisplayOrder = temporaryOrder;
        await context.SaveChangesAsync(ct);
        second.DisplayOrder = firstOrder;
        await context.SaveChangesAsync(ct);
        first.DisplayOrder = secondOrder;
        await context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task<IReadOnlyList<PhoneBookSearchRecord>> GetActiveSearchRecordsAsync(
        CancellationToken ct = default)
    {
        await using AppDbContext context = await contextFactory.CreateDbContextAsync(ct);
        return await context.PhoneBookEntries
            .AsNoTracking()
            .Where(entry => entry.IsActive && entry.Group.IsActive)
            .OrderBy(entry => entry.Group.DisplayOrder)
            .ThenBy(entry => entry.DisplayOrder)
            .Select(entry => new PhoneBookSearchRecord(
                entry.GroupId,
                entry.Group.Title,
                entry.Name,
                entry.Extension))
            .ToListAsync(ct);
    }

    private static PhoneBookGroup CopyGroup(PhoneBookGroup source)
    {
        return new PhoneBookGroup
        {
            Title = source.Title,
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
            Name = source.Name,
            Extension = source.Extension,
            DisplayOrder = source.DisplayOrder,
            IsActive = source.IsActive
        };
    }
}
