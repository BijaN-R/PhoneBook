using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PhoneBook.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PageWidthMm = table.Column<double>(type: "REAL", nullable: false),
                    PageHeightMm = table.Column<double>(type: "REAL", nullable: false),
                    MarginTopMm = table.Column<double>(type: "REAL", nullable: false),
                    MarginBottomMm = table.Column<double>(type: "REAL", nullable: false),
                    MarginLeftMm = table.Column<double>(type: "REAL", nullable: false),
                    MarginRightMm = table.Column<double>(type: "REAL", nullable: false),
                    GroupGapMm = table.Column<double>(type: "REAL", nullable: false),
                    CellPaddingMm = table.Column<double>(type: "REAL", nullable: false),
                    PrimaryFontFamily = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    UsePersianDigits = table.Column<bool>(type: "INTEGER", nullable: false),
                    MinFontSizePt = table.Column<double>(type: "REAL", nullable: false),
                    DefaultFontSizePt = table.Column<double>(type: "REAL", nullable: false),
                    HeaderFontSizePt = table.Column<double>(type: "REAL", nullable: false),
                    GroupHeaderFontSizePt = table.Column<double>(type: "REAL", nullable: false),
                    PriorityTopLimit = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentHeaders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Subtitle = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    UpdatedAt = table.Column<DateOnly>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentHeaders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PhoneBookGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    PreferredColumn = table.Column<int>(type: "INTEGER", nullable: true),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Required = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    KeepTogether = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhoneBookGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PhoneBookEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GroupId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Extension = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhoneBookEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhoneBookEntries_PhoneBookGroups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "PhoneBookGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AppSettings",
                columns: new[] { "Id", "CellPaddingMm", "DefaultFontSizePt", "GroupGapMm", "GroupHeaderFontSizePt", "HeaderFontSizePt", "MarginBottomMm", "MarginLeftMm", "MarginRightMm", "MarginTopMm", "MinFontSizePt", "PageHeightMm", "PageWidthMm", "PrimaryFontFamily", "PriorityTopLimit", "UsePersianDigits" },
                values: new object[] { 1, 1.2, 9.0, 2.5, 10.0, 16.0, 10.0, 8.0, 8.0, 10.0, 7.0, 297.0, 210.0, "Vazirmatn", 3, true });

            migrationBuilder.InsertData(
                table: "DocumentHeaders",
                columns: new[] { "Id", "Subtitle", "Title", "UpdatedAt" },
                values: new object[] { 1, null, "داخلی پرسنل شرکت رهیاب رایانه گستر", new DateOnly(2026, 1, 1) });

            migrationBuilder.InsertData(
                table: "PhoneBookGroups",
                columns: new[] { "Id", "DisplayOrder", "IsActive", "KeepTogether", "PreferredColumn", "Priority", "Required", "Title" },
                values: new object[,]
                {
                    { 1, 1, true, true, 0, 1, true, "فروش" },
                    { 2, 2, true, true, 0, 4, true, "نرم‌افزار پیام کوتاه" },
                    { 3, 3, true, true, 0, 7, true, "پشتیبانی پیام کوتاه" },
                    { 4, 4, true, true, 0, 9, true, "آبدارخانه" },
                    { 5, 5, true, true, 1, 2, true, "لجستیک" },
                    { 6, 6, true, true, 1, 10, true, "سامانه‌های عمومی" },
                    { 7, 7, true, true, 1, 11, true, "تست و تضمین کیفیت" },
                    { 8, 8, true, true, 1, 12, true, "طراحی محصول" },
                    { 9, 9, true, true, 1, 13, true, "امور قراردادها" },
                    { 10, 10, true, true, 1, 14, true, "دبیرخانه" },
                    { 11, 11, true, true, 1, 15, true, "حراست" },
                    { 12, 12, true, true, 1, 16, true, "اسکرام مستر" },
                    { 13, 13, true, true, 2, 3, true, "مدیریت" },
                    { 14, 14, true, true, 2, 5, true, "واحد مالی" },
                    { 15, 15, true, true, 2, 6, true, "واحد IT" },
                    { 16, 16, true, true, 2, 8, true, "واحد منابع انسانی" },
                    { 17, 17, true, true, 2, 17, true, "اتاق مشاوران" },
                    { 18, 18, true, true, 2, 18, true, "اتاق کنفرانس طبقه ۶" },
                    { 19, 19, true, true, 2, 19, true, "اتاق جلسات طبقه ۳" }
                });

            migrationBuilder.InsertData(
                table: "PhoneBookEntries",
                columns: new[] { "Id", "DisplayOrder", "Extension", "GroupId", "IsActive", "Name" },
                values: new object[,]
                {
                    { 1, 1, "۱۸۸", 1, true, "آقای جعفری" },
                    { 2, 2, "۱۹۶", 1, true, "خانم جهان‌نما" },
                    { 3, 3, "۱۹۳", 1, true, "خانم تقوائی" },
                    { 4, 4, "۱۹۵", 1, true, "خانم شمس" },
                    { 5, 5, "۱۹۴", 1, true, "خانم خیری" },
                    { 6, 6, "۱۹۸", 1, true, "خانم اردستانی" },
                    { 7, 1, "۱۱۶", 2, true, "آقای علی نوربخش" },
                    { 8, 2, "۱۱۵", 2, true, "خانم سمانه مومن" },
                    { 9, 3, "۱۲۲", 2, true, "آقای احسان جعفری" },
                    { 10, 4, "۱۱۸", 2, true, "آقای علی گودرزی" },
                    { 11, 5, "۱۲۴", 2, true, "آقای مهرابی" },
                    { 12, 6, "۱۱۹", 2, true, "آقای افشار" },
                    { 13, 7, "۱۲۳", 2, true, "آقای صالحان" },
                    { 14, 8, "۱۲۵", 2, true, "آقای عبدالهی" },
                    { 15, 9, "۱۲۰", 2, true, "خانم جعفر کیاه" },
                    { 16, 1, "۱۵۹", 3, true, "آقای نوید نصیری" },
                    { 17, 2, "۱۶۱", 3, true, "آقای مهدی خانی" },
                    { 18, 3, "۱۸۱", 3, true, "آقای علی کامجو" },
                    { 19, 4, "۱۵۶", 3, true, "آقای محمدجواد کریمی راد" },
                    { 20, 5, "۱۶۷", 3, true, "آقای نصرت رنجبر" },
                    { 21, 6, "۱۵۵", 3, true, "آقای بهبهانی" },
                    { 22, 7, "۱۶۰", 3, true, "خانم مسناآبادی" },
                    { 23, 8, "۱۸۳", 3, true, "آقای سامانی" },
                    { 24, 9, "۱۸۵", 3, true, "خانم میرزا زاده" },
                    { 25, 1, "۱۲۸", 4, true, "طبقه اول – علیرضا فتحی" },
                    { 26, 2, "۲۴۱", 4, true, "طبقه دوم – آقای بهروز شیرآوند" },
                    { 27, 3, "۳۱۷", 4, true, "طبقه سوم – آقای حسن شیرآوند" },
                    { 28, 4, "۵۹۰", 4, true, "" },
                    { 29, 5, "۶۱۳", 4, true, "طبقه ششم – آقای آیت شیراوند" },
                    { 30, 1, "۲۰۴", 5, true, "خانم قائم مقامی" },
                    { 31, 2, "۲۴۳", 5, true, "خانم حامدی" },
                    { 32, 3, "۲۴۲", 5, true, "خانم سلیمی" },
                    { 33, 4, "۲۳۲", 5, true, "آقای مزدارانی" },
                    { 34, 5, "۲۳۳", 5, true, "خانم نصیری زاده" },
                    { 35, 6, "۲۲۱", 5, true, "آقای روحی" },
                    { 36, 7, "۲۱۰", 5, true, "خانم خسروجردی" },
                    { 37, 8, "۲۱۹", 5, true, "آقای نوروزی" },
                    { 38, 9, "۲۲۲", 5, true, "بیژن راجی" },
                    { 39, 10, "۲۲۸", 5, true, "خانم خزچین" },
                    { 40, 11, "۲۳۱", 5, true, "آقای سعیدی نژاد" },
                    { 41, 1, "۲۲۶", 6, true, "" },
                    { 42, 1, "۱۳۴", 7, true, "آقای اسکندری" },
                    { 43, 1, "۱۶۳", 8, true, "خانم نوربخش" },
                    { 44, 2, "۱۹۷", 8, true, "خانم سلیمان پوریان" },
                    { 45, 1, "۵۸۷", 9, true, "آقای عسگری" },
                    { 46, 1, "۵۳۷", 10, true, "خانم صفری" },
                    { 47, 1, "۵۸۴", 11, true, "آقای استقامتی" },
                    { 48, 2, "۱۰۰", 11, true, "نگهبان" },
                    { 49, 1, "۱۲۱", 12, true, "" },
                    { 50, 1, "۶۰۱", 13, true, "خانم شاه محمدی" },
                    { 51, 2, "۶۰۷ - ۶۰۶", 13, true, "آقای دکتر شعاعی" },
                    { 52, 3, "۶۱۰", 13, true, "آقای باغستانی" },
                    { 53, 4, "۶۰۳", 13, true, "آقای باقری" },
                    { 54, 5, "۶۱۱", 13, true, "آقای دکتر فتحعلیزاده" },
                    { 55, 1, "۳۰۸", 14, true, "آقای قلانی" },
                    { 56, 2, "۳۰۷", 14, true, "خانم خردوار" },
                    { 57, 3, "۳۰۵", 14, true, "آقای داود آبادی" },
                    { 58, 4, "۳۰۶", 14, true, "خانم ذوالفقاری" },
                    { 59, 5, "۳۰۴", 14, true, "خانم طهماسبی" },
                    { 60, 6, "۳۱۶", 14, true, "آقای حبیبی" },
                    { 61, 7, "۳۲۱", 14, true, "آقای احمدی زاده" },
                    { 62, 8, "۳۰۹", 14, true, "آقای لطفی" },
                    { 63, 9, "۳۱۹", 14, true, "خانم علی نقیان" },
                    { 64, 1, "۴۰۲", 15, true, "آقای غریبی" },
                    { 65, 2, "۴۰۳", 15, true, "خانم زندیه" },
                    { 66, 3, "۴۰۱", 15, true, "آقای فیروزمنش" },
                    { 67, 4, "۴۰۵", 15, true, "آقای بیداران" },
                    { 68, 5, "۴۰۶", 15, true, "آقای ستاری کیا" },
                    { 69, 6, "۴۰۴", 15, true, "آقای پور صدرا" },
                    { 70, 7, null, 15, true, "خانم نژاد عبداله" },
                    { 71, 1, "۵۷۱", 16, true, "آقای خورش" },
                    { 72, 2, "۵۷۳", 16, true, "خانم گرشاسبی" },
                    { 73, 3, "۵۷۲", 16, true, "آقای عباسی" },
                    { 74, 4, "۵۷۷", 16, true, "خانم خزائلی" },
                    { 75, 5, "۵۳۸", 16, true, "خانم رحیمی" },
                    { 76, 1, "۶۰۴", 17, true, "" },
                    { 77, 1, "۶۰۹", 18, true, "" },
                    { 78, 1, "۳۱۱", 19, true, "" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_PhoneBookEntries_GroupId_DisplayOrder",
                table: "PhoneBookEntries",
                columns: new[] { "GroupId", "DisplayOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhoneBookEntries_Name",
                table: "PhoneBookEntries",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneBookGroups_DisplayOrder",
                table: "PhoneBookGroups",
                column: "DisplayOrder",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhoneBookGroups_Priority",
                table: "PhoneBookGroups",
                column: "Priority",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppSettings");

            migrationBuilder.DropTable(
                name: "DocumentHeaders");

            migrationBuilder.DropTable(
                name: "PhoneBookEntries");

            migrationBuilder.DropTable(
                name: "PhoneBookGroups");
        }
    }
}
