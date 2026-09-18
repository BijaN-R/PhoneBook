using PhoneBook.Application.Models;

namespace PhoneBook.Application.Abstractions;

public interface IPhoneBookDataQualityService
{
    Task<DataQualityReport> AnalyzeAsync(CancellationToken ct = default);
}
