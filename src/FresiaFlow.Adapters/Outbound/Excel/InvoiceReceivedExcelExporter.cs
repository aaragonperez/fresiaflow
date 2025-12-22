using FresiaFlow.Domain.InvoicesReceived;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;

namespace FresiaFlow.Adapters.Outbound.Excel;

/// <summary>
/// Servicio para exportar facturas recibidas a Excel.
/// Respeta los filtros aplicados y genera columnas contables claras.
/// </summary>
public class InvoiceReceivedExcelExporter
{
    private readonly ILogger<InvoiceReceivedExcelExporter> _logger;

    public InvoiceReceivedExcelExporter(ILogger<InvoiceReceivedExcelExporter> logger)
    {
        _logger = logger;
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    public async Task<byte[]> ExportToExcelAsync(
        List<InvoiceReceived> invoices,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Exportando {Count} facturas recibidas a Excel", invoices.Count);

        return await Task.Run(() =>
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Facturas Recibidas");

            // Encabezados contables
            var headers = new[]
            {
                "FECHA",
                "PROVEEDOR",
                "Nº FACTURA",
                "NIF/CIF",
                "BASE",
                "IVA",
                "TOTAL",
                "TIPO PAGO",
                "BANCO"
            };

            // Escribir encabezados
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cells[1, i + 1];
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            // Escribir datos
            for (int row = 0; row < invoices.Count; row++)
            {
                var invoice = invoices[row];
                var excelRow = row + 2; // +2 porque la fila 1 son encabezados

                worksheet.Cells[excelRow, 1].Value = invoice.IssueDate.ToString("yyyy-MM-dd");
                worksheet.Cells[excelRow, 2].Value = invoice.SupplierName;
                worksheet.Cells[excelRow, 3].Value = invoice.InvoiceNumber;
                worksheet.Cells[excelRow, 4].Value = invoice.SupplierTaxId ?? "";
                
                // Base imponible
                worksheet.Cells[excelRow, 5].Value = invoice.SubtotalAmount.Value;
                worksheet.Cells[excelRow, 5].Style.Numberformat.Format = "#,##0.00";
                
                // IVA
                worksheet.Cells[excelRow, 6].Value = invoice.TaxAmount?.Value ?? 0;
                worksheet.Cells[excelRow, 6].Style.Numberformat.Format = "#,##0.00";
                
                // Total
                worksheet.Cells[excelRow, 7].Value = invoice.TotalAmount.Value;
                worksheet.Cells[excelRow, 7].Style.Numberformat.Format = "#,##0.00";
                worksheet.Cells[excelRow, 7].Style.Font.Bold = true;
                
                // Tipo de pago
                worksheet.Cells[excelRow, 8].Value = invoice.PaymentType == PaymentType.Bank ? "Banco" : "Efectivo";
                
                // Banco (si aplica)
                var bankInfo = invoice.PaymentType == PaymentType.Bank && invoice.Payments.Any()
                    ? $"({invoice.Payments.Count} pago(s))"
                    : "";
                worksheet.Cells[excelRow, 9].Value = bankInfo;

                // Aplicar bordes a toda la fila
                for (int col = 1; col <= headers.Length; col++)
                {
                    worksheet.Cells[excelRow, col].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }
            }

            // Ajustar ancho de columnas
            worksheet.Cells.AutoFitColumns();

            // Asegurar que las columnas de números tengan ancho mínimo
            worksheet.Column(5).Width = Math.Max(worksheet.Column(5).Width, 15);
            worksheet.Column(6).Width = Math.Max(worksheet.Column(6).Width, 15);
            worksheet.Column(7).Width = Math.Max(worksheet.Column(7).Width, 15);

            // Agregar fila de totales
            var totalRow = invoices.Count + 3;
            worksheet.Cells[totalRow, 4].Value = "TOTALES:";
            worksheet.Cells[totalRow, 4].Style.Font.Bold = true;
            worksheet.Cells[totalRow, 5].Formula = $"SUM(E2:E{invoices.Count + 1})";
            worksheet.Cells[totalRow, 5].Style.Numberformat.Format = "#,##0.00";
            worksheet.Cells[totalRow, 5].Style.Font.Bold = true;
            worksheet.Cells[totalRow, 6].Formula = $"SUM(F2:F{invoices.Count + 1})";
            worksheet.Cells[totalRow, 6].Style.Numberformat.Format = "#,##0.00";
            worksheet.Cells[totalRow, 6].Style.Font.Bold = true;
            worksheet.Cells[totalRow, 7].Formula = $"SUM(G2:G{invoices.Count + 1})";
            worksheet.Cells[totalRow, 7].Style.Numberformat.Format = "#,##0.00";
            worksheet.Cells[totalRow, 7].Style.Font.Bold = true;

            _logger.LogInformation("Excel exportado con {Count} facturas recibidas", invoices.Count);

            return package.GetAsByteArray();
        }, cancellationToken);
    }
}

