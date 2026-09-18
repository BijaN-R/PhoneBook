using PhoneBook.Application.Abstractions;
using PhoneBook.Application.Abstractions.Persistence;
using PhoneBook.Application.Models;
using PhoneBook.Domain.Entities;

namespace PhoneBook.Application.Services;

public sealed class SettingsService(
    IAppSettingsRepository repository,
    IPhoneBookDataLock dataLock) : IDisposable
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
            _cached ??= await repository.GetAsync(ct)
                ?? throw new InvalidOperationException("The AppSettings row is missing from the database.");
            return Clone(_cached);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<AppSettings> ReloadAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            _cached = await repository.GetAsync(ct)
                ?? throw new InvalidOperationException("The AppSettings row is missing from the database.");
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
        await using IAsyncDisposable lease = await dataLock.AcquireAsync(ct);

        AppSettings requested = Clone(settings);
        if (requested.Id <= 0)
        {
            requested.Id = 1;
        }

        await _gate.WaitAsync(ct);
        try
        {
            if (await repository.GetAsync(ct) is null)
            {
                requested.Revision = 1;
                await repository.InsertAsync(requested, ct);
                _cached = Clone(requested);
            }
            else
            {
                AppSettings updated = await repository.UpdateAsync(
                    SettingsUpdateModel.FromEntity(requested),
                    ct);
                _cached = Clone(updated);
            }
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
            Revision = source.Revision,
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
}
