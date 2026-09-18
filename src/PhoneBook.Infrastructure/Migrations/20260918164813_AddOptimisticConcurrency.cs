using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhoneBook.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddOptimisticConcurrency : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<long>(
            name: "Revision",
            table: "PhoneBookGroups",
            type: "INTEGER",
            nullable: false,
            defaultValue: 1L);

        migrationBuilder.AddColumn<long>(
            name: "Revision",
            table: "PhoneBookEntries",
            type: "INTEGER",
            nullable: false,
            defaultValue: 1L);

        migrationBuilder.AddColumn<long>(
            name: "Revision",
            table: "AppSettings",
            type: "INTEGER",
            nullable: false,
            defaultValue: 1L);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "Revision", table: "PhoneBookGroups");
        migrationBuilder.DropColumn(name: "Revision", table: "PhoneBookEntries");
        migrationBuilder.DropColumn(name: "Revision", table: "AppSettings");
    }
}
