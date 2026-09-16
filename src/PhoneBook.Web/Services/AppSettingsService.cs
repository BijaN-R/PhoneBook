// FILE: src/PhoneBook.Web/Services/AppSettingsService.cs
using Microsoft.EntityFrameworkCore;
using PhoneBook.Domain.Entities;
using PhoneBook.Infrastructure.Data;

namespace PhoneBook.Web.Services;

public sealed class AppSettingsService(IDbContextFactory<AppDbContext> contextFactory) : IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private AppSettings? _cached;

    public async Task<AppSettings> GetAsync(CancellationToken ct = default)
    {
        if (_cached is not null)
        {
            return Clone(_cached);
        }

        await _gate.WaitAsync(ct);
        try
        {
            if (_cached is null)
            {
                await using AppDbContext context = await contextFactory.CreateDbContextAsync(ct);
                _cached = await context.AppSettings
                    .AsNoTracking()
                    .SingleOrDefaultAsync(ct)
                    ?? throw new InvalidOperationException("The AppSettings row is missing from the database.");
            }

            return Clone(_cached);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task UpdateAsync(AppSettings settings, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        AppSettings updated = Clone(settings);
        if (updated.Id <= 0)
        {
            updated.Id = 1;
        }

        await _gate.WaitAsync(ct);
        try
        {
            await using AppDbContext context = await contextFactory.CreateDbContextAsync(ct);
            AppSettings? existing = await context.AppSettings.SingleOrDefaultAsync(
                item => item.Id == updated.Id,
                ct);

            if (existing is null)
            {
                context.AppSettings.Add(updated);
            }
            else
            {
                CopyValues(updated, existing);
            }

            await context.SaveChangesAsync(ct);
            _cached = Clone(updated);
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose()
    {
        _gate.Dispose();
    }

    private static AppSettings Clone(AppSettings source)
    {
        return new AppSettings
        {
            Id = source.Id,
            PageWidthMm = source.PageWidthMm,
            PageHeightMm = source.PageHeightMm,
            MarginTopMm = source.MarginTopMm,
            MarginBottomMm = source.MarginBottomMm,
            MarginLeftMm = source.MarginLeftMm,
            MarginRightMm = source.MarginRightMm,
            GroupGapMm = source.GroupGapMm,
            CellPaddingMm = source.CellPaddingMm,
            PrimaryFontFamily = source.PrimaryFontFamily,
            UsePersianDigits = source.UsePersianDigits,
            MinFontSizePt = source.MinFontSizePt,
            DefaultFontSizePt = source.DefaultFontSizePt,
            HeaderFontSizePt = source.HeaderFontSizePt,
            GroupHeaderFontSizePt = source.GroupHeaderFontSizePt,
            PriorityTopLimit = source.PriorityTopLimit
        };
    }

    private static void CopyValues(AppSettings source, AppSettings destination)
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
}
