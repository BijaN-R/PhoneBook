using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PhoneBook.Application.Exceptions;
using PhoneBook.Application.Models;
using PhoneBook.Application.Services;
using PhoneBook.Domain.Entities;
using PhoneBook.Infrastructure.Data;
using PhoneBook.Infrastructure.Persistence;
using Xunit;

namespace PhoneBook.Tests;

public sealed class OperationalAdminTests
{
    [Fact]
    public async Task Required_group_cannot_be_hard_deleted()
    {
        await using TestDatabase db = await TestDatabase.CreateAsync();
        GroupManagementService service = CreateGroupService(db.Repository);
        PhoneBookGroup group = (await db.Repository.GetGroupsAsync()).First();

        InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.DeleteGroupAsync(group.Id, group.Revision));

        Assert.Contains("الزامی", error.Message);
    }

    [Fact]
    public async Task Non_required_group_with_entries_cannot_be_hard_deleted()
    {
        await using TestDatabase db = await TestDatabase.CreateAsync();
        GroupManagementService service = CreateGroupService(db.Repository);
        PhoneBookGroup group = (await db.Repository.GetGroupsAsync()).First();
        await service.UpdateGroupAsync(GroupUpdateModel.FromEntity(group) with { Required = false });
        group = (await db.Repository.GetGroupsAsync()).First();

        InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.DeleteGroupAsync(group.Id, group.Revision));

        Assert.Contains("دارای داخلی", error.Message);
    }

    [Fact]
    public async Task Duplicate_priority_is_rejected()
    {
        await using TestDatabase db = await TestDatabase.CreateAsync();
        GroupManagementService service = CreateGroupService(db.Repository);

        InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateGroupAsync(new PhoneBookGroup { Title = "تکراری", Priority = 1 }));

        Assert.Contains("اولویت", error.Message);
    }

    [Fact]
    public async Task Group_reorder_is_collision_safe_and_preserves_priority()
    {
        await using TestDatabase db = await TestDatabase.CreateAsync();
        IReadOnlyList<PhoneBookGroup> before = await db.Repository.GetGroupsAsync();

        await db.Repository.ReorderGroupAsync(before[2].Id, before[2].Revision, 0);

        IReadOnlyList<PhoneBookGroup> after = await db.Repository.GetGroupsAsync();
        Assert.Equal([3, 1, 2], after.Select(group => group.Id));
        Assert.Equal([1, 2, 3], after.Select(group => group.DisplayOrder));
        Assert.Equal(3, after[0].Priority);
    }

    [Fact]
    public async Task Bulk_state_change_is_all_or_nothing_on_concurrency_conflict()
    {
        await using TestDatabase db = await TestDatabase.CreateAsync();
        IReadOnlyList<PhoneBookEntry> original = await db.Repository.GetEntriesAsync(1);
        await db.Repository.UpdateEntryAsync(EntryUpdateModel.FromEntity(original[1]) with { Name = "تغییر یافته" });

        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => db.Repository.SetEntriesActiveStateAsync(
            original.Select(entry => new EntryStateChange(entry.Id, entry.Revision)).ToArray(), false));

        IReadOnlyList<PhoneBookEntry> current = await db.Repository.GetEntriesAsync(1);
        Assert.All(current, entry => Assert.True(entry.IsActive));
    }

    [Fact]
    public async Task Bulk_state_change_updates_every_selected_entry_in_one_operation()
    {
        await using TestDatabase db = await TestDatabase.CreateAsync();
        IReadOnlyList<PhoneBookEntry> original = await db.Repository.GetEntriesAsync(1);

        await db.Repository.SetEntriesActiveStateAsync(
            original.Take(2).Select(entry => new EntryStateChange(entry.Id, entry.Revision)).ToArray(), false);

        IReadOnlyList<PhoneBookEntry> current = await db.Repository.GetEntriesAsync(1);
        Assert.All(current.Take(2), entry => Assert.False(entry.IsActive));
        Assert.All(current.Skip(2), entry => Assert.True(entry.IsActive));
    }

    [Fact]
    public async Task Data_quality_detects_exact_repeated_and_one_edit_but_not_two_edits()
    {
        await using TestDatabase db = await TestDatabase.CreateAsync();
        DataQualityReport report = await new PhoneBookDataQualityService(db.Repository).AnalyzeAsync();

        Assert.Contains(report.Issues, issue => issue.Category == DataQualityCategory.ExactDuplicatePerson);
        Assert.Contains(report.Issues, issue => issue.Category == DataQualityCategory.RepeatedExtension);
        Assert.Contains(report.Issues, issue => issue.Category == DataQualityCategory.NearDuplicateName && issue.Message.Contains("نسیری"));
        Assert.DoesNotContain(report.Issues, issue => issue.Category == DataQualityCategory.NearDuplicateName && issue.Message.Contains("عسگری"));
    }

    private static GroupManagementService CreateGroupService(EfPhoneBookRepository repository)
    {
        PhoneBookSearchService search = new(repository);
        return new(repository, new PhoneBookDataLock(), search);
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly string _path;
        private TestDatabase(string path, EfPhoneBookRepository repository) { _path = path; Repository = repository; }
        public EfPhoneBookRepository Repository { get; }

        public static async Task<TestDatabase> CreateAsync()
        {
            string path = Path.Combine(Path.GetTempPath(), $"phonebook-operations-{Guid.NewGuid():N}.db");
            DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite($"Data Source={path}").Options;
            Factory factory = new(options);
            await using AppDbContext context = await factory.CreateDbContextAsync();
            await context.Database.EnsureCreatedAsync();
            context.PhoneBookGroups.AddRange(
                new PhoneBookGroup { Id=1, Title="فروش", Priority=1, DisplayOrder=1, Required=true, IsActive=true, Revision=1 },
                new PhoneBookGroup { Id=2, Title="اداری", Priority=2, DisplayOrder=2, Required=false, IsActive=true, Revision=1 },
                new PhoneBookGroup { Id=3, Title="فنی", Priority=3, DisplayOrder=3, Required=false, IsActive=true, Revision=1 });
            context.PhoneBookEntries.AddRange(
                Entry(1,"نصیری","188",1), Entry(2,"نسیری","188",2),
                Entry(3,"نصیری","188",3), Entry(4,"عسگری","189",4));
            await context.SaveChangesAsync();
            return new(path, new EfPhoneBookRepository(factory));
        }

        private static PhoneBookEntry Entry(int id,string name,string extension,int order) =>
            new() { Id=id, GroupId=1, Name=name, Extension=extension, DisplayOrder=order, IsActive=true, Revision=1 };
        public ValueTask DisposeAsync() { SqliteConnection.ClearAllPools(); if(File.Exists(_path)) File.Delete(_path); return ValueTask.CompletedTask; }
    }

    private sealed class Factory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext()=>new(options);
        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken=default)=>Task.FromResult(CreateDbContext());
    }
}
