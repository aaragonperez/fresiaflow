using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FresiaFlow.Adapters.Outbound.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefactorInvoiceReceivedToAccountingModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "InvoicesReceived");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "InvoicesReceived");

            migrationBuilder.RenameColumn(
                name: "ProcessedAt",
                table: "InvoicesReceived",
                newName: "UpdatedAt");

            migrationBuilder.AlterColumn<string>(
                name: "SubtotalCurrency",
                table: "InvoicesReceived",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "SubtotalAmount",
                table: "InvoicesReceived",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "InvoicesReceived",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "ExtractionConfidence",
                table: "InvoicesReceived",
                type: "numeric(3,2)",
                precision: 3,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Origin",
                table: "InvoicesReceived",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PaymentType",
                table: "InvoicesReceived",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ReceivedDate",
                table: "InvoicesReceived",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "SupplierAddress",
                table: "InvoicesReceived",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxRate",
                table: "InvoicesReceived",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InvoiceReceivedPayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceReceivedId = table.Column<Guid>(type: "uuid", nullable: false),
                    BankTransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InvoiceReceivedId1 = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceReceivedPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceReceivedPayments_InvoicesReceived_InvoiceReceivedId",
                        column: x => x.InvoiceReceivedId,
                        principalTable: "InvoicesReceived",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InvoiceReceivedPayments_InvoicesReceived_InvoiceReceivedId1",
                        column: x => x.InvoiceReceivedId1,
                        principalTable: "InvoicesReceived",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IssuedInvoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Series = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    InvoiceNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IssueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TaxableBase = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxableBaseCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxAmountCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmountCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    ClientId = table.Column<string>(type: "text", nullable: false),
                    ClientTaxId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ClientName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PostalCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Province = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Country = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false, defaultValue: "ES"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SourceFilePath = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssuedInvoices", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceReceivedPayments_BankTransactionId",
                table: "InvoiceReceivedPayments",
                column: "BankTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceReceivedPayments_InvoiceReceivedId",
                table: "InvoiceReceivedPayments",
                column: "InvoiceReceivedId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceReceivedPayments_InvoiceReceivedId1",
                table: "InvoiceReceivedPayments",
                column: "InvoiceReceivedId1");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceReceivedPayments_PaymentDate",
                table: "InvoiceReceivedPayments",
                column: "PaymentDate");

            migrationBuilder.CreateIndex(
                name: "IX_IssuedInvoices_Series_InvoiceNumber",
                table: "IssuedInvoices",
                columns: new[] { "Series", "InvoiceNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvoiceReceivedPayments");

            migrationBuilder.DropTable(
                name: "IssuedInvoices");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "InvoicesReceived");

            migrationBuilder.DropColumn(
                name: "ExtractionConfidence",
                table: "InvoicesReceived");

            migrationBuilder.DropColumn(
                name: "Origin",
                table: "InvoicesReceived");

            migrationBuilder.DropColumn(
                name: "PaymentType",
                table: "InvoicesReceived");

            migrationBuilder.DropColumn(
                name: "ReceivedDate",
                table: "InvoicesReceived");

            migrationBuilder.DropColumn(
                name: "SupplierAddress",
                table: "InvoicesReceived");

            migrationBuilder.DropColumn(
                name: "TaxRate",
                table: "InvoicesReceived");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "InvoicesReceived",
                newName: "ProcessedAt");

            migrationBuilder.AlterColumn<string>(
                name: "SubtotalCurrency",
                table: "InvoicesReceived",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3);

            migrationBuilder.AlterColumn<decimal>(
                name: "SubtotalAmount",
                table: "InvoicesReceived",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "InvoicesReceived",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "InvoicesReceived",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }
    }
}
