using PhoneBook.Domain.Entities;

namespace PhoneBook.Application.Abstractions.Persistence;

public interface IAppSettingsRepository
{
    Task<AppSettings?> GetAsync(CancellationToken ct = default);

    Task InsertAsync(AppSettings settings, CancellationToken ct = default);

    Task UpdateAsync(AppSettings settings, CancellationToken ct = default);
}
