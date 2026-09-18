using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PhoneBook.Infrastructure.Data;

#nullable disable

namespace PhoneBook.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260915161853_init")]
public partial class InitialCreate : Migration
{
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
            constraints: table => table.PrimaryKey("PK_AppSettings", x => x.Id));

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
            constraints: table => table.PrimaryKey("PK_DocumentHeaders", x => x.Id));

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
            constraints: table => table.PrimaryKey("PK_PhoneBookGroups", x => x.Id));

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
            columns:
            [
                "Id", "CellPaddingMm", "DefaultFontSizePt", "GroupGapMm",
                "GroupHeaderFontSizePt", "HeaderFontSizePt", "MarginBottomMm",
                "MarginLeftMm", "MarginRightMm", "MarginTopMm", "MinFontSizePt",
                "PageHeightMm", "PageWidthMm", "PrimaryFontFamily",
                "PriorityTopLimit", "UsePersianDigits"
            ],
            columnTypes:
            [
                "INTEGER", "REAL", "REAL", "REAL", "REAL", "REAL", "REAL",
                "REAL", "REAL", "REAL", "REAL", "REAL", "REAL", "TEXT",
                "INTEGER", "INTEGER"
            ],
            values: new object[]
            {
                1, 1.2, 9.0, 2.5, 10.0, 16.0, 10.0, 8.0, 8.0, 10.0,
                7.0, 297.0, 210.0, "Vazirmatn", 3, true
            });

        migrationBuilder.InsertData(
            table: "DocumentHeaders",
            columns: ["Id", "Subtitle", "Title", "UpdatedAt"],
            columnTypes: ["INTEGER", "TEXT", "TEXT", "TEXT"],
            values: new object[] { 1, null, "دفترچه تلفن سازمانی", new DateOnly(2026, 1, 1) });

        migrationBuilder.CreateIndex(
            name: "IX_PhoneBookEntries_GroupId_DisplayOrder",
            table: "PhoneBookEntries",
            columns: ["GroupId", "DisplayOrder"],
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

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AppSettings");
        migrationBuilder.DropTable(name: "DocumentHeaders");
        migrationBuilder.DropTable(name: "PhoneBookEntries");
        migrationBuilder.DropTable(name: "PhoneBookGroups");
    }
}
