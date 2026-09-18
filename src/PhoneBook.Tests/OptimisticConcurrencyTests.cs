using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using PhoneBook.Application.Exceptions;
using PhoneBook.Application.Models;
using PhoneBook.Domain.Entities;
using PhoneBook.Infrastructure.Data;
using PhoneBook.Infrastructure.Persistence;
using Xunit;

namespace PhoneBook.Tests;

public sealed class OptimisticConcurrencyTests
{
    [Fact]
    public async Task Stale_group_update_is_rejected()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        EfPhoneBookRepository repository = new(database.Factory);
        PhoneBookGroup original = (await repository.GetGroupsAsync()).Single();

        await repository.UpdateGroupAsync(
            GroupUpdateModel.FromEntity(original) with { Title = "نسخه جدید" });

        Func<Task> staleUpdate = () => repository.UpdateGroupAsync(
            GroupUpdateModel.FromEntity(original) with { Title = "نسخه قدیمی" });

        await Assert.ThrowsAsync<ConcurrencyConflictException>(staleUpdate);
        PhoneBookGroup current = (await repository.GetGroupsAsync()).Single();
        Assert.Equal("نسخه جدید", current.Title);
        Assert.Equal(2, current.Revision);
    }

    [Fact]
    public async Task Stale_group_delete_is_rejected()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        EfPhoneBookRepository repository = new(database.Factory);
        PhoneBookGroup original = (await repository.GetGroupsAsync()).Single();

        await repository.UpdateGroupAsync(
            GroupUpdateModel.FromEntity(original) with { Title = "تغییر یافته" });

        Func<Task> staleDelete = () => repository.DeleteGroupAsync(
            original.Id,
            original.Revision);

        await Assert.ThrowsAsync<ConcurrencyConflictException>(staleDelete);
        Assert.Single(await repository.GetGroupsAsync());
    }

    [Fact]
    public async Task Entry_swap_rolls_back_when_either_revision_is_stale()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync(includeEntries: true);
        EfPhoneBookRepository repository = new(database.Factory);
        IReadOnlyList<PhoneBookEntry> entries = await repository.GetEntriesAsync(1);

        await repository.UpdateEntryAsync(
            EntryUpdateModel.FromEntity(entries[1]) with { Name = "تغییر یافته" });

        Func<Task> staleMove = () => repository.SwapEntryDisplayOrdersAsync(
            entries[0].Id,
            entries[0].Revision,
            entries[1].Id,
            entries[1].Revision);

        await Assert.ThrowsAsync<ConcurrencyConflictException>(staleMove);
        IReadOnlyList<PhoneBookEntry> current = await repository.GetEntriesAsync(1);
        Assert.Equal([1, 2], current.Select(entry => entry.DisplayOrder));
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly string _path;

        private TestDatabase(string path, TestDbContextFactory factory)
        {
            _path = path;
            Factory = factory;
        }

        public TestDbContextFactory Factory { get; }

        public static async Task<TestDatabase> CreateAsync(bool includeEntries = false)
        {
            string path = Path.Combine(Path.GetTempPath(), $"phonebook-concurrency-{Guid.NewGuid():N}.db");
            DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={path}")
                .Options;
            TestDbContextFactory factory = new(options);

            await using AppDbContext context = await factory.CreateDbContextAsync();
            await context.Database.EnsureCreatedAsync();
            context.PhoneBookGroups.Add(new PhoneBookGroup
            {
                Id = 1,
                Title = "گروه",
                Priority = 1,
                DisplayOrder = 1,
                Revision = 1
            });
            if (includeEntries)
            {
                context.PhoneBookEntries.AddRange(
                    new PhoneBookEntry
                    {
                        Id = 1,
                        GroupId = 1,
                        Name = "یک",
                        DisplayOrder = 1,
                        Revision = 1
                    },
                    new PhoneBookEntry
                    {
                        Id = 2,
                        GroupId = 1,
                        Name = "دو",
                        DisplayOrder = 2,
                        Revision = 1
                    });
            }
            await context.SaveChangesAsync();
            return new TestDatabase(path, factory);
        }

        public ValueTask DisposeAsync()
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(_path))
            {
                File.Delete(_path);
            }
            return ValueTask.CompletedTask;
        }
    }

    private sealed class TestDbContextFactory(DbContextOptions<AppDbContext> options)
        : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateDbContext());
        }
    }
}
