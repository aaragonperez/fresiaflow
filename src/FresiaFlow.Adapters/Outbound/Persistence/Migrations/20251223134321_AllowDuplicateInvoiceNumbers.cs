using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FresiaFlow.Adapters.Outbound.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AllowDuplicateInvoiceNumbers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InvoicesReceived_InvoiceNumber",
                table: "InvoicesReceived");

            migrationBuilder.CreateIndex(
                name: "IX_InvoicesReceived_InvoiceNumber",
                table: "InvoicesReceived",
                column: "InvoiceNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InvoicesReceived_InvoiceNumber",
                table: "InvoicesReceived");

            migrationBuilder.CreateIndex(
                name: "IX_InvoicesReceived_InvoiceNumber",
                table: "InvoicesReceived",
                column: "InvoiceNumber",
                unique: true);
        }
    }
}
