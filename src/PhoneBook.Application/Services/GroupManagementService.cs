using PhoneBook.Application.Abstractions;
using PhoneBook.Application.Abstractions.Persistence;
using PhoneBook.Application.Exceptions;
using PhoneBook.Application.Models;
using PhoneBook.Domain.Entities;

namespace PhoneBook.Application.Services;

public sealed class GroupManagementService(
    IPhoneBookRepository repository,
    IPhoneBookDataLock dataLock,
    PhoneBookSearchService searchService)
{
    private const int OrderingRetryLimit = 3;

    public async Task<PhoneBookGroup> CreateGroupAsync(
        PhoneBookGroup group,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(group);
        await using IAsyncDisposable lease = await dataLock.AcquireAsync(ct);

        if (group.Priority > 0 && (await repository.GetGroupsAsync(false, ct)).Any(item => item.Priority == group.Priority))
        {
            throw new InvalidOperationException("اولویت انتخاب‌شده قبلاً استفاده شده است.");
        }

        bool automaticDisplayOrder = group.DisplayOrder <= 0;
        bool automaticPriority = group.Priority <= 0;
        for (int attempt = 1; attempt <= OrderingRetryLimit; attempt++)
        {
            PhoneBookGroup created = Copy(group);
            if (automaticDisplayOrder)
            {
                created.DisplayOrder = await repository.GetMaximumGroupDisplayOrderAsync(ct) + 1;
            }

            if (automaticPriority)
            {
                created.Priority = await repository.GetMaximumGroupPriorityAsync(ct) + 1;
            }

            try
            {
                PhoneBookGroup inserted = await repository.InsertGroupAsync(created, ct);
                await searchService.RefreshAsync(ct);
                return inserted;
            }
            catch (OrderingConflictException) when (attempt < OrderingRetryLimit
                && (automaticDisplayOrder || automaticPriority))
            {
            }
        }

        throw new OrderingConflictException(
            "Could not assign a unique group order after several attempts.");
    }

    public async Task UpdateGroupAsync(GroupUpdateModel group, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(group);
        await using IAsyncDisposable lease = await dataLock.AcquireAsync(ct);
        if ((await repository.GetGroupsAsync(false, ct)).Any(item => item.Id != group.Id && item.Priority == group.Priority))
        {
            throw new InvalidOperationException("اولویت انتخاب‌شده قبلاً استفاده شده است.");
        }
        GroupUpdateModel updated = group with { Title = group.Title.Trim() };
        await repository.UpdateGroupAsync(updated, ct);
        await searchService.RefreshAsync(ct);
    }

    public async Task DeleteGroupAsync(int id, long expectedRevision, CancellationToken ct = default)
    {
        await using IAsyncDisposable lease = await dataLock.AcquireAsync(ct);
        PhoneBookGroup group = await repository.GetGroupAsync(id, ct)
            ?? throw new ConcurrencyConflictException("The group was already deleted.");
        if (group.Revision != expectedRevision)
        {
            throw new ConcurrencyConflictException("The group changed after it was loaded.");
        }
        if (group.Required)
        {
            throw new InvalidOperationException("گروه الزامی قابل حذف نیست؛ ابتدا وضعیت الزامی را غیرفعال کنید.");
        }
        if (group.Entries.Count > 0)
        {
            throw new InvalidOperationException("گروهی که دارای داخلی است قابل حذف نیست؛ ابتدا داخلی‌های آن را حذف کنید.");
        }
        await repository.DeleteGroupAsync(id, expectedRevision, ct);
        await searchService.RefreshAsync(ct);
    }

    public async Task MoveGroupAsync(int id, long expectedRevision, int offset, CancellationToken ct = default)
    {
        if (offset is not (-1 or 1)) throw new ArgumentOutOfRangeException(nameof(offset));
        await using IAsyncDisposable lease = await dataLock.AcquireAsync(ct);
        IReadOnlyList<PhoneBookGroup> groups = await repository.GetGroupsAsync(false, ct);
        int index = groups.ToList().FindIndex(group => group.Id == id);
        int target = index + offset;
        if (index < 0) throw new ConcurrencyConflictException("The group was deleted.");
        if (target < 0 || target >= groups.Count) return;
        await repository.ReorderGroupAsync(id, expectedRevision, target, ct);
        await searchService.RefreshAsync(ct);
    }

    public async Task MoveGroupToBoundaryAsync(int id, long expectedRevision, bool toTop, CancellationToken ct = default)
    {
        await using IAsyncDisposable lease = await dataLock.AcquireAsync(ct);
        IReadOnlyList<PhoneBookGroup> groups = await repository.GetGroupsAsync(false, ct);
        if (!groups.Any(group => group.Id == id)) throw new ConcurrencyConflictException("The group was deleted.");
        await repository.ReorderGroupAsync(id, expectedRevision, toTop ? 0 : groups.Count - 1, ct);
        await searchService.RefreshAsync(ct);
    }

    private static PhoneBookGroup Copy(PhoneBookGroup source)
    {
        return new PhoneBookGroup
        {
            Title = source.Title.Trim(),
            Priority = source.Priority,
            PreferredColumn = source.PreferredColumn,
            DisplayOrder = source.DisplayOrder,
            Required = source.Required,
            KeepTogether = source.KeepTogether,
            IsActive = source.IsActive,
            Revision = 1
        };
    }
}
