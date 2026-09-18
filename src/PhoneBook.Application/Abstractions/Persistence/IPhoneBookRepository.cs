using PhoneBook.Application.Models;
using PhoneBook.Domain.Entities;

namespace PhoneBook.Application.Abstractions.Persistence;

public interface IPhoneBookRepository
{
    Task<DocumentHeader> GetDocumentHeaderAsync(CancellationToken ct = default);

    Task<IReadOnlyList<PhoneBookGroup>> GetGroupsAsync(
        bool activeOnly = false,
        CancellationToken ct = default);

    Task<PhoneBookGroup?> GetGroupAsync(int id, CancellationToken ct = default);

    Task<IReadOnlyList<PhoneBookEntry>> GetEntriesAsync(
        int groupId,
        bool activeOnly = false,
        CancellationToken ct = default);

    Task<PhoneBookEntry?> GetEntryAsync(int id, CancellationToken ct = default);

    Task<int> GetMaximumGroupDisplayOrderAsync(CancellationToken ct = default);

    Task<int> GetMaximumGroupPriorityAsync(CancellationToken ct = default);

    Task<int> GetMaximumEntryDisplayOrderAsync(int groupId, CancellationToken ct = default);

    Task<PhoneBookGroup> InsertGroupAsync(PhoneBookGroup group, CancellationToken ct = default);

    Task UpdateGroupAsync(PhoneBookGroup group, CancellationToken ct = default);

    Task DeleteGroupAsync(int id, CancellationToken ct = default);

    Task<PhoneBookEntry> InsertEntryAsync(PhoneBookEntry entry, CancellationToken ct = default);

    Task UpdateEntryAsync(PhoneBookEntry entry, CancellationToken ct = default);

    Task DeleteEntryAsync(int id, CancellationToken ct = default);

    Task SwapEntryDisplayOrdersAsync(
        int firstEntryId,
        int secondEntryId,
        CancellationToken ct = default);

    Task<IReadOnlyList<PhoneBookSearchRecord>> GetActiveSearchRecordsAsync(
        CancellationToken ct = default);
}
