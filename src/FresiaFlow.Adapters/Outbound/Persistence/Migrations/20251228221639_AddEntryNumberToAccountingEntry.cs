using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FresiaFlow.Adapters.Outbound.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEntryNumberToAccountingEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EntryNumber",
                table: "AccountingEntries",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EntryYear",
                table: "AccountingEntries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Establecer EntryYear basado en EntryDate para registros existentes
            migrationBuilder.Sql(@"
                UPDATE ""AccountingEntries""
                SET ""EntryYear"" = EXTRACT(YEAR FROM ""EntryDate"")
                WHERE ""EntryYear"" = 0;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingEntries_EntryYear_EntryNumber",
                table: "AccountingEntries",
                columns: new[] { "EntryYear", "EntryNumber" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AccountingEntries_EntryYear_EntryNumber",
                table: "AccountingEntries");

            migrationBuilder.DropColumn(
                name: "EntryNumber",
                table: "AccountingEntries");

            migrationBuilder.DropColumn(
                name: "EntryYear",
                table: "AccountingEntries");
        }
    }
}
