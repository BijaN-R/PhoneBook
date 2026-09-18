using Microsoft.EntityFrameworkCore;
using PhoneBook.Application.Abstractions.Persistence;
using PhoneBook.Application.Exceptions;
using PhoneBook.Application.Models;
using PhoneBook.Domain.Entities;
using PhoneBook.Infrastructure.Data;

namespace PhoneBook.Infrastructure.Persistence;

public sealed class EfAppSettingsRepository(IDbContextFactory<AppDbContext> contextFactory)
    : IAppSettingsRepository
{
    public async Task<AppSettings?> GetAsync(CancellationToken ct = default)
    {
        await using AppDbContext context = await contextFactory.CreateDbContextAsync(ct);
        return await context.AppSettings.AsNoTracking().SingleOrDefaultAsync(ct);
    }

    public async Task InsertAsync(AppSettings settings, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        await using AppDbContext context = await contextFactory.CreateDbContextAsync(ct);
        context.AppSettings.Add(Clone(settings));
        await context.SaveChangesAsync(ct);
    }

    public async Task<AppSettings> UpdateAsync(SettingsUpdateModel settings, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        await using AppDbContext context = await contextFactory.CreateDbContextAsync(ct);
        AppSettings? existing = await context.AppSettings.SingleOrDefaultAsync(
            item => item.Id == settings.Id,
            ct);
        if (existing is null)
        {
            throw new ConcurrencyConflictException("The settings were deleted by another administrator.");
        }

        context.Entry(existing).Property(item => item.Revision).OriginalValue = settings.ExpectedRevision;
        CopyValues(settings, existing);
        existing.Revision = settings.ExpectedRevision + 1;
        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrencyConflictException(
                "The settings changed after they were loaded.",
                exception);
        }

        return Clone(existing);
    }

    private static AppSettings Clone(AppSettings source)
    {
        AppSettings clone = new() { Id = source.Id, Revision = source.Revision };
        CopyValues(source, clone);
        return clone;
    }

    private static void CopyValues(SettingsUpdateModel source, AppSettings destination)
    {
        destination.PageWidthMm = source.PageWidthMm;
        destination.PageHeightMm = source.PageHeightMm;
        destination.MarginTopMm = source.MarginTopMm;
        destination.MarginBottomMm = source.MarginBottomMm;
        destination.MarginLeftMm = source.MarginLeftMm;
        destination.MarginRightMm = source.MarginRightMm;
        destination.GroupGapMm = source.GroupGapMm;
        destination.CellPaddingMm = source.CellPaddingMm;
        destination.PrimaryFontFamily = source.PrimaryFontFamily;
        destination.UsePersianDigits = source.UsePersianDigits;
        destination.MinFontSizePt = source.MinFontSizePt;
        destination.DefaultFontSizePt = source.DefaultFontSizePt;
        destination.HeaderFontSizePt = source.HeaderFontSizePt;
        destination.GroupHeaderFontSizePt = source.GroupHeaderFontSizePt;
        destination.PriorityTopLimit = source.PriorityTopLimit;
    }

    private static void CopyValues(AppSettings source, AppSettings destination)
    {
        CopyValues(SettingsUpdateModel.FromEntity(source), destination);
    }
}
