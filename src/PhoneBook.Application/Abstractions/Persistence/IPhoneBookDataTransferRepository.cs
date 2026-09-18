using PhoneBook.Application.Models;

namespace PhoneBook.Application.Abstractions.Persistence;

public interface IPhoneBookDataTransferRepository
{
    Task<PhoneBookTransferDocument> LoadSnapshotAsync(CancellationToken ct = default);

    Task ReplaceAsync(PhoneBookTransferDocument document, CancellationToken ct = default);
}
