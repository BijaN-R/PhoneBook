using PhoneBook.Domain.Entities;
using PhoneBook.Application.Models;

namespace PhoneBook.Application.Abstractions.Persistence;

public interface IAppSettingsRepository
{
    Task<AppSettings?> GetAsync(CancellationToken ct = default);

    Task InsertAsync(AppSettings settings, CancellationToken ct = default);

    Task<AppSettings> UpdateAsync(SettingsUpdateModel settings, CancellationToken ct = default);
}
