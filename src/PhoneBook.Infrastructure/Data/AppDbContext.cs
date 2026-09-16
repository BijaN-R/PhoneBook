// FILE: src/PhoneBook.Infrastructure/Data/AppDbContext.cs
using Microsoft.EntityFrameworkCore;
using PhoneBook.Domain.Entities;
using PhoneBook.Domain.Enums;

namespace PhoneBook.Infrastructure.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
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
        SeedData(modelBuilder);
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

    private static void SeedData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DocumentHeader>().HasData(
            new DocumentHeader
            {
                Id = 1,
                Title = "داخلی پرسنل شرکت رهیاب رایانه گستر",
                Subtitle = null,
                UpdatedAt = new DateOnly(2026, 1, 1)
            });

        modelBuilder.Entity<AppSettings>().HasData(new AppSettings { Id = 1 });

        modelBuilder.Entity<PhoneBookGroup>().HasData(
            Group(1, "فروش", 1, ColumnPosition.Right, 1),
            Group(2, "نرم‌افزار پیام کوتاه", 4, ColumnPosition.Right, 2),
            Group(3, "پشتیبانی پیام کوتاه", 7, ColumnPosition.Right, 3),
            Group(4, "آبدارخانه", 9, ColumnPosition.Right, 4),
            Group(5, "لجستیک", 2, ColumnPosition.Middle, 5),
            Group(6, "سامانه‌های عمومی", 10, ColumnPosition.Middle, 6),
            Group(7, "تست و تضمین کیفیت", 11, ColumnPosition.Middle, 7),
            Group(8, "طراحی محصول", 12, ColumnPosition.Middle, 8),
            Group(9, "امور قراردادها", 13, ColumnPosition.Middle, 9),
            Group(10, "دبیرخانه", 14, ColumnPosition.Middle, 10),
            Group(11, "حراست", 15, ColumnPosition.Middle, 11),
            Group(12, "اسکرام مستر", 16, ColumnPosition.Middle, 12),
            Group(13, "مدیریت", 3, ColumnPosition.Left, 13),
            Group(14, "واحد مالی", 5, ColumnPosition.Left, 14),
            Group(15, "واحد IT", 6, ColumnPosition.Left, 15),
            Group(16, "واحد منابع انسانی", 8, ColumnPosition.Left, 16),
            Group(17, "اتاق مشاوران", 17, ColumnPosition.Left, 17),
            Group(18, "اتاق کنفرانس طبقه ۶", 18, ColumnPosition.Left, 18),
            Group(19, "اتاق جلسات طبقه ۳", 19, ColumnPosition.Left, 19));

        modelBuilder.Entity<PhoneBookEntry>().HasData(
            Entry(1, 1, "آقای جعفری", "۱۸۸", 1),
            Entry(2, 1, "خانم جهان‌نما", "۱۹۶", 2),
            Entry(3, 1, "خانم تقوائی", "۱۹۳", 3),
            Entry(4, 1, "خانم شمس", "۱۹۵", 4),
            Entry(5, 1, "خانم خیری", "۱۹۴", 5),
            Entry(6, 1, "خانم اردستانی", "۱۹۸", 6),

            Entry(7, 2, "آقای علی نوربخش", "۱۱۶", 1),
            Entry(8, 2, "خانم سمانه مومن", "۱۱۵", 2),
            Entry(9, 2, "آقای احسان جعفری", "۱۲۲", 3),
            Entry(10, 2, "آقای علی گودرزی", "۱۱۸", 4),
            Entry(11, 2, "آقای مهرابی", "۱۲۴", 5),
            Entry(12, 2, "آقای افشار", "۱۱۹", 6),
            Entry(13, 2, "آقای صالحان", "۱۲۳", 7),
            Entry(14, 2, "آقای عبدالهی", "۱۲۵", 8),
            Entry(15, 2, "خانم جعفر کیاه", "۱۲۰", 9),

            Entry(16, 3, "آقای نوید نصیری", "۱۵۹", 1),
            Entry(17, 3, "آقای مهدی خانی", "۱۶۱", 2),
            Entry(18, 3, "آقای علی کامجو", "۱۸۱", 3),
            Entry(19, 3, "آقای محمدجواد کریمی راد", "۱۵۶", 4),
            Entry(20, 3, "آقای نصرت رنجبر", "۱۶۷", 5),
            Entry(21, 3, "آقای بهبهانی", "۱۵۵", 6),
            Entry(22, 3, "خانم مسناآبادی", "۱۶۰", 7),
            Entry(23, 3, "آقای سامانی", "۱۸۳", 8),
            Entry(24, 3, "خانم میرزا زاده", "۱۸۵", 9),

            Entry(25, 4, "طبقه اول – علیرضا فتحی", "۱۲۸", 1),
            Entry(26, 4, "طبقه دوم – آقای بهروز شیرآوند", "۲۴۱", 2),
            Entry(27, 4, "طبقه سوم – آقای حسن شیرآوند", "۳۱۷", 3),
            Entry(28, 4, string.Empty, "۵۹۰", 4),
            Entry(29, 4, "طبقه ششم – آقای آیت شیراوند", "۶۱۳", 5),

            Entry(30, 5, "خانم قائم مقامی", "۲۰۴", 1),
            Entry(31, 5, "خانم حامدی", "۲۴۳", 2),
            Entry(32, 5, "خانم سلیمی", "۲۴۲", 3),
            Entry(33, 5, "آقای مزدارانی", "۲۳۲", 4),
            Entry(34, 5, "خانم نصیری زاده", "۲۳۳", 5),
            Entry(35, 5, "آقای روحی", "۲۲۱", 6),
            Entry(36, 5, "خانم خسروجردی", "۲۱۰", 7),
            Entry(37, 5, "آقای نوروزی", "۲۱۹", 8),
            Entry(38, 5, "بیژن راجی", "۲۲۲", 9),
            Entry(39, 5, "خانم خزچین", "۲۲۸", 10),
            Entry(40, 5, "آقای سعیدی نژاد", "۲۳۱", 11),

            Entry(41, 6, string.Empty, "۲۲۶", 1),
            Entry(42, 7, "آقای اسکندری", "۱۳۴", 1),
            Entry(43, 8, "خانم نوربخش", "۱۶۳", 1),
            Entry(44, 8, "خانم سلیمان پوریان", "۱۹۷", 2),
            Entry(45, 9, "آقای عسگری", "۵۸۷", 1),
            Entry(46, 10, "خانم صفری", "۵۳۷", 1),
            Entry(47, 11, "آقای استقامتی", "۵۸۴", 1),
            Entry(48, 11, "نگهبان", "۱۰۰", 2),
            Entry(49, 12, string.Empty, "۱۲۱", 1),

            Entry(50, 13, "خانم شاه محمدی", "۶۰۱", 1),
            Entry(51, 13, "آقای دکتر شعاعی", "۶۰۷ - ۶۰۶", 2),
            Entry(52, 13, "آقای باغستانی", "۶۱۰", 3),
            Entry(53, 13, "آقای باقری", "۶۰۳", 4),
            Entry(54, 13, "آقای دکتر فتحعلیزاده", "۶۱۱", 5),

            Entry(55, 14, "آقای قلانی", "۳۰۸", 1),
            Entry(56, 14, "خانم خردوار", "۳۰۷", 2),
            Entry(57, 14, "آقای داود آبادی", "۳۰۵", 3),
            Entry(58, 14, "خانم ذوالفقاری", "۳۰۶", 4),
            Entry(59, 14, "خانم طهماسبی", "۳۰۴", 5),
            Entry(60, 14, "آقای حبیبی", "۳۱۶", 6),
            Entry(61, 14, "آقای احمدی زاده", "۳۲۱", 7),
            Entry(62, 14, "آقای لطفی", "۳۰۹", 8),
            Entry(63, 14, "خانم علی نقیان", "۳۱۹", 9),

            Entry(64, 15, "آقای غریبی", "۴۰۲", 1),
            Entry(65, 15, "خانم زندیه", "۴۰۳", 2),
            Entry(66, 15, "آقای فیروزمنش", "۴۰۱", 3),
            Entry(67, 15, "آقای بیداران", "۴۰۵", 4),
            Entry(68, 15, "آقای ستاری کیا", "۴۰۶", 5),
            Entry(69, 15, "آقای پور صدرا", "۴۰۴", 6),
            Entry(70, 15, "خانم نژاد عبداله", null, 7),

            Entry(71, 16, "آقای خورش", "۵۷۱", 1),
            Entry(72, 16, "خانم گرشاسبی", "۵۷۳", 2),
            Entry(73, 16, "آقای عباسی", "۵۷۲", 3),
            Entry(74, 16, "خانم خزائلی", "۵۷۷", 4),
            Entry(75, 16, "خانم رحیمی", "۵۳۸", 5),

            Entry(76, 17, string.Empty, "۶۰۴", 1),
            Entry(77, 18, string.Empty, "۶۰۹", 1),
            Entry(78, 19, string.Empty, "۳۱۱", 1));
    }

    private static PhoneBookGroup Group(
        int id,
        string title,
        int priority,
        ColumnPosition preferredColumn,
        int displayOrder)
    {
        return new PhoneBookGroup
        {
            Id = id,
            Title = title,
            Priority = priority,
            PreferredColumn = preferredColumn,
            DisplayOrder = displayOrder,
            Required = true,
            KeepTogether = true,
            IsActive = true
        };
    }

    private static PhoneBookEntry Entry(
        int id,
        int groupId,
        string name,
        string? extension,
        int displayOrder)
    {
        return new PhoneBookEntry
        {
            Id = id,
            GroupId = groupId,
            Name = name,
            Extension = extension,
            DisplayOrder = displayOrder,
            IsActive = true
        };
    }
}
