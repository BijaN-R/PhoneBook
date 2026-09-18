using Microsoft.EntityFrameworkCore;
using PhoneBook.Domain.Entities;

namespace PhoneBook.Infrastructure.Data;

public sealed class DefaultPhoneBookDataInitializer(IDbContextFactory<AppDbContext> contextFactory)
{
    private static readonly string[] DefaultGroupTitles =
    [
        "مدیریت",
        "منابع انسانی",
        "واحد مالی",
        "لجستیک",
        "نرم‌افزار پیام کوتاه",
        "پشتیبانی پیام کوتاه",
        "واحد IT",
        "حراست"
    ];

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await using AppDbContext context = await contextFactory.CreateDbContextAsync(ct);
        await using var transaction = await context.Database.BeginTransactionAsync(ct);
        if (await context.PhoneBookGroups.AnyAsync(ct))
        {
            await transaction.CommitAsync(ct);
            return;
        }

        for (int index = 0; index < DefaultGroupTitles.Length; index++)
        {
            int order = index + 1;
            context.PhoneBookGroups.Add(new PhoneBookGroup
            {
                Title = DefaultGroupTitles[index],
                Priority = order,
                DisplayOrder = order,
                Revision = 1,
                Required = true,
                KeepTogether = true,
                IsActive = true
            });
        }

        await context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }
}
