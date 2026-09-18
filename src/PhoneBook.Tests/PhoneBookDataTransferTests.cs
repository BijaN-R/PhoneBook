using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PhoneBook.Application.Models;
using PhoneBook.Application.Services;
using PhoneBook.Domain.Entities;
using PhoneBook.Infrastructure.Data;
using PhoneBook.Infrastructure.Identity;
using PhoneBook.Infrastructure.Persistence;
using PhoneBook.Infrastructure.Serialization;
using Xunit;

namespace PhoneBook.Tests;

public sealed class PhoneBookDataTransferTests
{
    [Fact]
    public async Task Export_validate_and_restore_preserve_business_data_but_not_identity_payload()
    {
        await using TransferTestDatabase database = await TransferTestDatabase.CreateAsync();
        EfPhoneBookRepository phoneBookRepository = new(database.Factory);
        PhoneBookDataLock dataLock = new();
        SettingsService settingsService = new(
            new EfAppSettingsRepository(database.Factory),
            dataLock);
        PhoneBookDataTransferService service = new(
            new EfPhoneBookDataTransferRepository(database.Factory),
            new JsonPhoneBookTransferSerializer(),
            dataLock,
            settingsService,
            new PhoneBookSearchService(phoneBookRepository));

        PhoneBookExportResult export = await service.ExportAsync();
        string json = Encoding.UTF8.GetString(export.Content);
        Assert.Contains("\"schemaVersion\": 1", json);
        Assert.DoesNotContain("revision", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("passwordHash", json, StringComparison.OrdinalIgnoreCase);

        PhoneBookImportPreview preview = await service.ValidateImportAsync(export.Content);
        Assert.True(preview.CanApply);
        Assert.Equal(1, preview.Summary.GroupCount);
        Assert.Equal(1, preview.Summary.EntryCount);

        await service.ApplyImportAsync(preview.Document!);

        await using AppDbContext context = await database.Factory.CreateDbContextAsync();
        Assert.Equal("گروه اصلی", (await context.PhoneBookGroups.SingleAsync()).Title);
        Assert.Equal("۱۲۳", (await context.PhoneBookEntries.SingleAsync()).Extension);
        Assert.Equal("admin@example.com", (await context.Users.SingleAsync()).Email);
    }

    [Fact]
    public async Task Failed_database_replacement_rolls_back_all_business_data()
    {
        await using TransferTestDatabase database = await TransferTestDatabase.CreateAsync();
        EfPhoneBookDataTransferRepository repository = new(database.Factory);
        PhoneBookTransferDocument snapshot = await repository.LoadSnapshotAsync();
        PhoneBookTransferGroup group = snapshot.Groups!.Single();
        PhoneBookTransferDocument invalid = snapshot with
        {
            Groups = [group, group with { Title = "گروه دوم" }]
        };

        await Assert.ThrowsAsync<DbUpdateException>(() => repository.ReplaceAsync(invalid));

        await using AppDbContext context = await database.Factory.CreateDbContextAsync();
        Assert.Equal("گروه اصلی", (await context.PhoneBookGroups.SingleAsync()).Title);
        Assert.Single(await context.PhoneBookEntries.ToListAsync());
        Assert.Single(await context.Users.ToListAsync());
    }

    private sealed class TransferTestDatabase : IAsyncDisposable
    {
        private readonly string _path;

        private TransferTestDatabase(string path, TransferDbContextFactory factory)
        {
            _path = path;
            Factory = factory;
        }

        public TransferDbContextFactory Factory { get; }

        public static async Task<TransferTestDatabase> CreateAsync()
        {
            string path = Path.Combine(Path.GetTempPath(), $"phonebook-transfer-{Guid.NewGuid():N}.db");
            DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={path}")
                .Options;
            TransferDbContextFactory factory = new(options);
            await using AppDbContext context = await factory.CreateDbContextAsync();
            await context.Database.EnsureCreatedAsync();
            context.PhoneBookGroups.Add(new PhoneBookGroup
            {
                Title = "گروه اصلی",
                Priority = 1,
                DisplayOrder = 1,
                Entries =
                [
                    new PhoneBookEntry
                    {
                        Name = "کاربر نمونه",
                        Extension = "۱۲۳",
                        DisplayOrder = 1
                    }
                ]
            });
            context.Users.Add(new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "admin@example.com",
                NormalizedUserName = "ADMIN@EXAMPLE.COM",
                Email = "admin@example.com",
                NormalizedEmail = "ADMIN@EXAMPLE.COM",
                PasswordHash = "not-exported"
            });
            await context.SaveChangesAsync();
            return new TransferTestDatabase(path, factory);
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

    private sealed class TransferDbContextFactory(DbContextOptions<AppDbContext> options)
        : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateDbContext());
        }
    }
}
