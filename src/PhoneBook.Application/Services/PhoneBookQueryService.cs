using PhoneBook.Application.Abstractions.Persistence;
using PhoneBook.Domain.Entities;

namespace PhoneBook.Application.Services;

public sealed class PhoneBookQueryService(IPhoneBookRepository repository)
{
    public Task<DocumentHeader> GetDocumentHeaderAsync(CancellationToken ct = default)
    {
        return repository.GetDocumentHeaderAsync(ct);
    }

    public Task<IReadOnlyList<PhoneBookGroup>> GetGroupsAsync(
        bool activeOnly = false,
        CancellationToken ct = default)
    {
        return repository.GetGroupsAsync(activeOnly, ct);
    }

    public Task<PhoneBookGroup?> GetGroupAsync(int id, CancellationToken ct = default)
    {
        return repository.GetGroupAsync(id, ct);
    }

    public Task<IReadOnlyList<PhoneBookEntry>> GetEntriesAsync(
        int groupId,
        bool activeOnly = false,
        CancellationToken ct = default)
    {
        return repository.GetEntriesAsync(groupId, activeOnly, ct);
    }
}
