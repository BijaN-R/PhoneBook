using FluentAssertions;
using PhoneBook.Application.Abstractions.Persistence;
using PhoneBook.Application.Models;
using PhoneBook.Application.Services;
using PhoneBook.Domain.Entities;
using Xunit;

namespace PhoneBook.Tests;

public sealed class PhoneBookSearchServiceTests
{
    [Fact]
    public async Task Persian_One_Edit_Matches_But_Unrelated_Two_Edit_Name_Does_Not()
    {
        PhoneBookSearchService service = await CreateServiceAsync(
            Record(1, "آقای نصیری"), Record(2, "آقای عسگری"));

        service.Search("نسیری").Select(x => x.EntryId).Should().Equal(1);
        service.Search("آقای نسیری").Select(x => x.EntryId).Should().Equal(1);
    }

    [Fact]
    public async Task Exact_Name_Ranks_Above_Fuzzy_Name()
    {
        PhoneBookSearchService service = await CreateServiceAsync(
            Record(1, "نصیری"), Record(2, "نسیری"));

        service.Search("نسیری").Select(x => x.EntryId).Should().Equal(2, 1);
    }

    [Fact]
    public async Task Exact_Extension_Ranks_First_And_Numbers_Are_Never_Fuzzy()
    {
        PhoneBookSearchService service = await CreateServiceAsync(
            Record(1, "یک", "189"), Record(2, "دو", "188"), Record(3, "188 کارشناس", "999"));

        IReadOnlyList<SearchHit> results = service.Search("188");

        results.Select(x => x.EntryId).Should().Equal(2, 3);
        results.Should().NotContain(x => x.EntryId == 1);
        results[0].MatchKind.Should().Be(SearchMatchKind.ExactExtension);
    }

    [Fact]
    public async Task Prefix_Ranks_Above_Substring()
    {
        PhoneBookSearchService service = await CreateServiceAsync(
            Record(1, "پیشفروش"), Record(2, "فروشگاه"));

        service.Search("فروش").Select(x => x.EntryId).Should().Equal(2, 1);
    }

    [Fact]
    public async Task Multi_Token_Uses_Weakest_Match_And_Requires_Every_Token()
    {
        PhoneBookSearchService service = await CreateServiceAsync(
            Record(1, "علی نصیری"), Record(2, "علی نسیری"), Record(3, "علی رضایی"));

        service.Search("علی نسیری").Select(x => x.EntryId).Should().Equal(2, 1);
        service.Search("علی نسیری").Should().NotContain(x => x.EntryId == 3);
    }

    [Fact]
    public async Task Text_And_Number_Can_Match_Across_Fields_And_Intent_Word_Is_Ignored()
    {
        PhoneBookSearchService service = await CreateServiceAsync(
            Record(1, "کارشناس", "188", "فروش"), Record(2, "فروش", "189", "اداری"));

        service.Search("فروش 188").Select(x => x.EntryId).Should().Equal(1);
        service.Search("داخلی ۱۸۸").Select(x => x.EntryId).Should().Equal(1);
    }

    [Fact]
    public async Task Title_Only_Query_Is_Exact_And_Not_Empty()
    {
        PhoneBookSearchService service = await CreateServiceAsync(
            Record(1, "آقای احمدی"), Record(2, "اقای رضایی"), Record(3, "خانم محمدی"));

        service.Search("آقای").Select(x => x.EntryId).Should().Equal(1);
    }

    [Fact]
    public async Task Empty_Query_Uses_Deterministic_Display_Order()
    {
        PhoneBookSearchService service = await CreateServiceAsync(
            Record(1, "سوم", groupOrder: 2, entryOrder: 1),
            Record(2, "دوم", groupOrder: 1, entryOrder: 2),
            Record(3, "اول", groupOrder: 1, entryOrder: 1));

        service.Search(" ").Select(x => x.EntryId).Should().Equal(3, 2, 1);
    }

    [Fact]
    public async Task Osa_Adjacent_Transposition_Is_Accepted()
    {
        PhoneBookSearchService service = await CreateServiceAsync(Record(1, "کتاب"));

        service.Search("کتبا").Select(x => x.EntryId).Should().Equal(1);
    }

    [Fact]
    public async Task Distance_Two_Is_Rejected_For_Short_Tokens()
    {
        PhoneBookSearchService service = await CreateServiceAsync(Record(1, "عسگری"));

        service.Search("نسیری").Should().BeEmpty();
    }

    private static async Task<PhoneBookSearchService> CreateServiceAsync(params PhoneBookSearchRecord[] records)
    {
        PhoneBookSearchService service = new(new SearchRepository(records));
        await service.RefreshAsync();
        return service;
    }

    private static PhoneBookSearchRecord Record(int id, string name, string? extension = null,
        string group = "عمومی", int groupOrder = 1, int entryOrder = 1) =>
        new(id, groupOrder, group, name, extension, groupOrder, entryOrder);

    private sealed class SearchRepository(IReadOnlyList<PhoneBookSearchRecord> records) : IPhoneBookRepository
    {
        public Task<IReadOnlyList<PhoneBookSearchRecord>> GetActiveSearchRecordsAsync(CancellationToken ct = default) => Task.FromResult(records);
        public Task<DocumentHeader> GetDocumentHeaderAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<PhoneBookGroup>> GetGroupsAsync(bool activeOnly = false, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<PhoneBookGroup?> GetGroupAsync(int id, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<PhoneBookEntry>> GetEntriesAsync(int groupId, bool activeOnly = false, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<PhoneBookEntry?> GetEntryAsync(int id, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<int> GetMaximumGroupDisplayOrderAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<int> GetMaximumGroupPriorityAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<int> GetMaximumEntryDisplayOrderAsync(int groupId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<PhoneBookGroup> InsertGroupAsync(PhoneBookGroup group, CancellationToken ct = default) => throw new NotSupportedException();
        public Task UpdateGroupAsync(GroupUpdateModel group, CancellationToken ct = default) => throw new NotSupportedException();
        public Task DeleteGroupAsync(int id, long expectedRevision, CancellationToken ct = default) => throw new NotSupportedException();
        public Task ReorderGroupAsync(int id, long expectedRevision, int targetIndex, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<PhoneBookEntry> InsertEntryAsync(PhoneBookEntry entry, CancellationToken ct = default) => throw new NotSupportedException();
        public Task UpdateEntryAsync(EntryUpdateModel entry, CancellationToken ct = default) => throw new NotSupportedException();
        public Task DeleteEntryAsync(int id, long expectedRevision, CancellationToken ct = default) => throw new NotSupportedException();
        public Task SwapEntryDisplayOrdersAsync(int firstEntryId, long firstExpectedRevision, int secondEntryId,
            long secondExpectedRevision, CancellationToken ct = default) => throw new NotSupportedException();
        public Task ReorderEntryAsync(int id, long expectedRevision, int targetIndex, CancellationToken ct = default) => throw new NotSupportedException();
        public Task SetEntriesActiveStateAsync(IReadOnlyList<EntryStateChange> entries, bool isActive, CancellationToken ct = default) => throw new NotSupportedException();
    }
}
