using Microsoft.EntityFrameworkCore;
using PhoneBook.Application.Abstractions.Persistence;
using PhoneBook.Application.Models;
using PhoneBook.Application.Services;
using PhoneBook.Domain.Entities;
using PhoneBook.Infrastructure.Data;

namespace PhoneBook.Infrastructure.Persistence;

public sealed class EfPhoneBookDataTransferRepository(
    IDbContextFactory<AppDbContext> contextFactory) : IPhoneBookDataTransferRepository
{
    public async Task<PhoneBookTransferDocument> LoadSnapshotAsync(CancellationToken ct = default)
    {
        await using AppDbContext context = await contextFactory.CreateDbContextAsync(ct);
        await using var transaction = await context.Database.BeginTransactionAsync(ct);

        DocumentHeader header = await context.DocumentHeaders.AsNoTracking().SingleAsync(ct);
        AppSettings settings = await context.AppSettings.AsNoTracking().SingleAsync(ct);
        List<PhoneBookGroup> groups = await context.PhoneBookGroups
            .AsNoTracking()
            .Include(group => group.Entries)
            .OrderBy(group => group.DisplayOrder)
            .ThenBy(group => group.Id)
            .ToListAsync(ct);

        await transaction.CommitAsync(ct);
        return new PhoneBookTransferDocument(
            PhoneBookDataTransferService.CurrentSchemaVersion,
            DateTimeOffset.UtcNow,
            "PhoneBook",
            new PhoneBookTransferHeader(header.Title, header.Subtitle, header.UpdatedAt),
            MapSettings(settings),
            groups.Select(MapGroup).ToArray());
    }

    public async Task ReplaceAsync(
        PhoneBookTransferDocument document,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        PhoneBookTransferHeader headerData = document.DocumentHeader
            ?? throw new InvalidOperationException("The validated header is missing.");
        PhoneBookTransferSettings settingsData = document.AppSettings
            ?? throw new InvalidOperationException("The validated settings are missing.");
        IReadOnlyList<PhoneBookTransferGroup> groups = document.Groups
            ?? throw new InvalidOperationException("The validated groups are missing.");

        await using AppDbContext context = await contextFactory.CreateDbContextAsync(ct);
        await using var transaction = await context.Database.BeginTransactionAsync(ct);

        DocumentHeader header = await context.DocumentHeaders.SingleAsync(ct);
        AppSettings settings = await context.AppSettings.SingleAsync(ct);

        await context.PhoneBookEntries.ExecuteDeleteAsync(ct);
        await context.PhoneBookGroups.ExecuteDeleteAsync(ct);

        header.Title = headerData.Title!.Trim();
        header.Subtitle = string.IsNullOrWhiteSpace(headerData.Subtitle)
            ? null
            : headerData.Subtitle.Trim();
        header.UpdatedAt = headerData.UpdatedAt;

        CopySettings(settingsData, settings);
        settings.Revision++;

        foreach (PhoneBookTransferGroup groupData in groups)
        {
            PhoneBookGroup group = new()
            {
                Title = groupData.Title!.Trim(),
                Priority = groupData.Priority,
                PreferredColumn = groupData.PreferredColumn,
                DisplayOrder = groupData.DisplayOrder,
                Required = groupData.Required,
                KeepTogether = groupData.KeepTogether,
                IsActive = groupData.IsActive,
                Revision = 1
            };
            foreach (PhoneBookTransferEntry entryData in groupData.Entries ?? [])
            {
                group.Entries.Add(new PhoneBookEntry
                {
                    Name = entryData.Name?.Trim() ?? string.Empty,
                    Extension = string.IsNullOrWhiteSpace(entryData.Extension)
                        ? null
                        : entryData.Extension.Trim(),
                    DisplayOrder = entryData.DisplayOrder,
                    IsActive = entryData.IsActive,
                    Revision = 1
                });
            }
            context.PhoneBookGroups.Add(group);
        }

        await context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    private static PhoneBookTransferSettings MapSettings(AppSettings settings)
    {
        return new(
            settings.PageWidthMm,
            settings.PageHeightMm,
            settings.MarginTopMm,
            settings.MarginBottomMm,
            settings.MarginLeftMm,
            settings.MarginRightMm,
            settings.GroupGapMm,
            settings.CellPaddingMm,
            settings.PrimaryFontFamily,
            settings.UsePersianDigits,
            settings.MinFontSizePt,
            settings.DefaultFontSizePt,
            settings.HeaderFontSizePt,
            settings.GroupHeaderFontSizePt,
            settings.PriorityTopLimit);
    }

    private static PhoneBookTransferGroup MapGroup(PhoneBookGroup group)
    {
        return new(
            group.Title,
            group.Priority,
            group.PreferredColumn,
            group.DisplayOrder,
            group.Required,
            group.KeepTogether,
            group.IsActive,
            group.Entries
                .OrderBy(entry => entry.DisplayOrder)
                .ThenBy(entry => entry.Id)
                .Select(entry => new PhoneBookTransferEntry(
                    entry.Name,
                    entry.Extension,
                    entry.DisplayOrder,
                    entry.IsActive))
                .ToArray());
    }

    private static void CopySettings(
        PhoneBookTransferSettings source,
        AppSettings destination)
    {
        destination.PageWidthMm = source.PageWidthMm;
        destination.PageHeightMm = source.PageHeightMm;
        destination.MarginTopMm = source.MarginTopMm;
        destination.MarginBottomMm = source.MarginBottomMm;
        destination.MarginLeftMm = source.MarginLeftMm;
        destination.MarginRightMm = source.MarginRightMm;
        destination.GroupGapMm = source.GroupGapMm;
        destination.CellPaddingMm = source.CellPaddingMm;
        destination.PrimaryFontFamily = source.PrimaryFontFamily!.Trim();
        destination.UsePersianDigits = source.UsePersianDigits;
        destination.MinFontSizePt = source.MinFontSizePt;
        destination.DefaultFontSizePt = source.DefaultFontSizePt;
        destination.HeaderFontSizePt = source.HeaderFontSizePt;
        destination.GroupHeaderFontSizePt = source.GroupHeaderFontSizePt;
        destination.PriorityTopLimit = source.PriorityTopLimit;
    }
}
