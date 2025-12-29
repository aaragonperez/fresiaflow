using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FresiaFlow.Adapters.Outbound.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIrpfToInvoices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Tasks",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPinned",
                table: "Tasks",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "IrpfAmount",
                table: "InvoicesReceived",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IrpfCurrency",
                table: "InvoicesReceived",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "IrpfRate",
                table: "InvoicesReceived",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_IsCompleted_IsPinned_Priority",
                table: "Tasks",
                columns: new[] { "IsCompleted", "IsPinned", "Priority" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tasks_IsCompleted_IsPinned_Priority",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "IsPinned",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "IrpfAmount",
                table: "InvoicesReceived");

            migrationBuilder.DropColumn(
                name: "IrpfCurrency",
                table: "InvoicesReceived");

            migrationBuilder.DropColumn(
                name: "IrpfRate",
                table: "InvoicesReceived");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Tasks",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);
        }
    }
}
