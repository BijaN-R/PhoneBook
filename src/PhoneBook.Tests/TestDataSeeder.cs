// FILE: src/PhoneBook.Tests/TestDataSeeder.cs
using PhoneBook.Core.Layout;
using PhoneBook.Domain.Entities;
using PhoneBook.Domain.Enums;

namespace PhoneBook.Tests;

public static class TestDataSeeder
{
    public static DocumentHeader CreateHeader()
    {
        return new DocumentHeader
        {
            Id = 1,
            Title = "داخلی پرسنل شرکت رهیاب رایانه گستر",
            Subtitle = null,
            UpdatedAt = new DateOnly(2026, 1, 1)
        };
    }

    public static AppSettings CreateSettings()
    {
        return new AppSettings { Id = 1 };
    }

    public static IReadOnlyList<PhoneBookGroup> CreateSeedGroups()
    {
        List<PhoneBookGroup> groups =
        [
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
            Group(19, "اتاق جلسات طبقه ۳", 19, ColumnPosition.Left, 19)
        ];

        PhoneBookEntry[] entries =
        [
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
            Entry(78, 19, string.Empty, "۳۱۱", 1)
        ];

        Dictionary<int, PhoneBookGroup> byId = groups.ToDictionary(group => group.Id);
        foreach (PhoneBookEntry entry in entries)
        {
            entry.Group = byId[entry.GroupId];
            byId[entry.GroupId].Entries.Add(entry);
        }

        return groups;
    }

    public static IReadOnlyList<PhoneBookGroup> CreateGroups(
        int groupCount,
        int rowsPerGroup,
        bool includePriorities = false)
    {
        if (groupCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(groupCount));
        }

        if (rowsPerGroup < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rowsPerGroup));
        }

        List<PhoneBookGroup> groups = new(groupCount);
        int entryId = 1;
        for (int groupIndex = 1; groupIndex <= groupCount; groupIndex++)
        {
            PhoneBookGroup group = new()
            {
                Id = groupIndex,
                Title = $"گروه {groupIndex}",
                Priority = includePriorities && groupIndex <= 4 ? groupIndex : 0,
                DisplayOrder = groupIndex,
                IsActive = true,
                Required = true,
                KeepTogether = true
            };

            for (int rowIndex = 1; rowIndex <= rowsPerGroup; rowIndex++)
            {
                PhoneBookEntry entry = new()
                {
                    Id = entryId++,
                    GroupId = group.Id,
                    Group = group,
                    Name = $"همکار {groupIndex}-{rowIndex}",
                    Extension = (100 + rowIndex).ToString(System.Globalization.CultureInfo.InvariantCulture),
                    DisplayOrder = rowIndex,
                    IsActive = true
                };
                group.Entries.Add(entry);
            }

            groups.Add(group);
        }

        return groups;
    }

    public static string GetFontsPath()
    {
        string? directory = AppContext.BaseDirectory;
        while (directory is not null && !File.Exists(Path.Combine(directory, "phase_6.md")))
        {
            directory = Directory.GetParent(directory)?.FullName;
        }

        if (directory is null)
        {
            throw new DirectoryNotFoundException("Could not locate the PhoneBook repository root.");
        }

        return Path.Combine(directory, "src", "PhoneBook.Web", "wwwroot", "fonts");
    }

    public sealed class DeterministicTextMeasurer : ITextMeasurer
    {
        private const double MillimetersPerPoint = 25.4 / 72.0;

        public double MeasureTextWidthMm(string text, string fontFamily, double fontSizePt)
        {
            ArgumentNullException.ThrowIfNull(text);
            return text.Length * fontSizePt * 0.5 * MillimetersPerPoint;
        }

        public double MeasureTextHeightMm(string fontFamily, double fontSizePt)
        {
            return fontSizePt * MillimetersPerPoint;
        }
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
