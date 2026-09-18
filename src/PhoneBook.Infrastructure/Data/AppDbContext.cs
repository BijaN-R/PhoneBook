// FILE: src/PhoneBook.Infrastructure/Data/AppDbContext.cs
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PhoneBook.Domain.Entities;
using PhoneBook.Infrastructure.Identity;

namespace PhoneBook.Infrastructure.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<DocumentHeader> DocumentHeaders => Set<DocumentHeader>();
    public DbSet<PhoneBookGroup> PhoneBookGroups => Set<PhoneBookGroup>();
    public DbSet<PhoneBookEntry> PhoneBookEntries => Set<PhoneBookEntry>();
    public DbSet<AppSettings> AppSettings => Set<AppSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ConfigureDocumentHeader(modelBuilder);
        ConfigurePhoneBookGroup(modelBuilder);
        ConfigurePhoneBookEntry(modelBuilder);
        ConfigureAppSettings(modelBuilder);
        SeedTechnicalDefaults(modelBuilder);
    }

    private static void ConfigureDocumentHeader(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DocumentHeader>(entity =>
        {
            entity.HasKey(header => header.Id);
            entity.Property(header => header.Title).IsRequired().HasMaxLength(200);
            entity.Property(header => header.Subtitle).HasMaxLength(500);
            entity.Property(header => header.UpdatedAt).IsRequired();
        });
    }

    private static void ConfigurePhoneBookGroup(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PhoneBookGroup>(entity =>
        {
            entity.HasKey(group => group.Id);
            entity.HasIndex(group => group.DisplayOrder).IsUnique();
            entity.HasIndex(group => group.Priority).IsUnique();
            entity.Property(group => group.Title).IsRequired().HasMaxLength(150);
            entity.Property(group => group.Priority).IsRequired();
            entity.Property(group => group.DisplayOrder).IsRequired();
            entity.Property(group => group.Revision).IsRequired().IsConcurrencyToken();
            entity.Property(group => group.Required).IsRequired().HasDefaultValue(true);
            entity.Property(group => group.KeepTogether).IsRequired().HasDefaultValue(true);
            entity.Property(group => group.IsActive).IsRequired().HasDefaultValue(true);
        });
    }

    private static void ConfigurePhoneBookEntry(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PhoneBookEntry>(entity =>
        {
            entity.HasKey(entry => entry.Id);
            entity.HasIndex(entry => new { entry.GroupId, entry.DisplayOrder }).IsUnique();
            entity.HasIndex(entry => entry.Name);
            entity.Property(entry => entry.Name).IsRequired().HasMaxLength(200);
            entity.Property(entry => entry.Extension).HasMaxLength(50);
            entity.Property(entry => entry.DisplayOrder).IsRequired();
            entity.Property(entry => entry.Revision).IsRequired().IsConcurrencyToken();
            entity.Property(entry => entry.IsActive).IsRequired().HasDefaultValue(true);
            entity.HasOne(entry => entry.Group)
                .WithMany(group => group.Entries)
                .HasForeignKey(entry => entry.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureAppSettings(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppSettings>(entity =>
        {
            entity.HasKey(settings => settings.Id);
            entity.Property(settings => settings.Revision).IsRequired().IsConcurrencyToken();
            entity.Property(settings => settings.PageWidthMm).IsRequired();
            entity.Property(settings => settings.PageHeightMm).IsRequired();
            entity.Property(settings => settings.MarginTopMm).IsRequired();
            entity.Property(settings => settings.MarginBottomMm).IsRequired();
            entity.Property(settings => settings.MarginLeftMm).IsRequired();
            entity.Property(settings => settings.MarginRightMm).IsRequired();
            entity.Property(settings => settings.GroupGapMm).IsRequired();
            entity.Property(settings => settings.CellPaddingMm).IsRequired();
            entity.Property(settings => settings.PrimaryFontFamily).IsRequired().HasMaxLength(100);
            entity.Property(settings => settings.UsePersianDigits).IsRequired();
            entity.Property(settings => settings.MinFontSizePt).IsRequired();
            entity.Property(settings => settings.DefaultFontSizePt).IsRequired();
            entity.Property(settings => settings.HeaderFontSizePt).IsRequired();
            entity.Property(settings => settings.GroupHeaderFontSizePt).IsRequired();
            entity.Property(settings => settings.PriorityTopLimit).IsRequired();
        });
    }

    private static void SeedTechnicalDefaults(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DocumentHeader>().HasData(new DocumentHeader
        {
            Id = 1,
            Title = "دفترچه تلفن سازمانی",
            Subtitle = null,
            UpdatedAt = new DateOnly(2026, 1, 1)
        });
        modelBuilder.Entity<AppSettings>().HasData(new AppSettings { Id = 1, Revision = 1 });
    }
}
