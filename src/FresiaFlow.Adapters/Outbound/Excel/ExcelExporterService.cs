using FresiaFlow.Application.Ports.Outbound;
using FresiaFlow.Domain.Invoices;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;

namespace FresiaFlow.Adapters.Outbound.Excel;

/// <summary>
/// Servicio para exportar facturas emitidas a Excel.
/// </summary>
public class ExcelExporterService : IExcelExporter
{
    private readonly ILogger<ExcelExporterService> _logger;

    public ExcelExporterService(ILogger<ExcelExporterService> logger)
    {
        _logger = logger;
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    public async Task<byte[]> ExportToExcelAsync(
        List<IssuedInvoice> invoices,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Exportando {Count} facturas a Excel", invoices.Count);

        return await Task.Run(() =>
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Facturas");

            // Encabezados
            var headers = new[]
            {
                "SERIE",
                "FECHA FACTURA",
                "Nº FACTURA",
                "BASE IMPONIBLE",
                "IVA",
                "TOTAL FACTURAS",
                "ID CLIENTE",
                "NIF",
                "NOMBRE CLIENTE",
                "DOMICILIO",
                "LOCALIDAD",
                "COD. POSTAL",
                "PROVINCIA",
                "PAÍS"
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
            }

            // Escribir datos
            for (int row = 0; row < invoices.Count; row++)
            {
                var invoice = invoices[row];
                var excelRow = row + 2; // +2 porque la fila 1 son encabezados

                worksheet.Cells[excelRow, 1].Value = invoice.Series;
                worksheet.Cells[excelRow, 2].Value = invoice.IssueDate.ToString("yyyy-MM-dd");
                worksheet.Cells[excelRow, 3].Value = invoice.InvoiceNumber;
                worksheet.Cells[excelRow, 4].Value = invoice.TaxableBase.Value;
                worksheet.Cells[excelRow, 4].Style.Numberformat.Format = "#,##0.00";
                worksheet.Cells[excelRow, 5].Value = invoice.TaxAmount.Value;
                worksheet.Cells[excelRow, 5].Style.Numberformat.Format = "#,##0.00";
                worksheet.Cells[excelRow, 6].Value = invoice.TotalAmount.Value;
                worksheet.Cells[excelRow, 6].Style.Numberformat.Format = "#,##0.00";
                worksheet.Cells[excelRow, 7].Value = invoice.ClientId;
                worksheet.Cells[excelRow, 8].Value = invoice.ClientTaxId;
                worksheet.Cells[excelRow, 9].Value = invoice.ClientName;
                worksheet.Cells[excelRow, 10].Value = invoice.Address;
                worksheet.Cells[excelRow, 11].Value = invoice.City;
                worksheet.Cells[excelRow, 12].Value = invoice.PostalCode;
                worksheet.Cells[excelRow, 13].Value = invoice.Province;
                worksheet.Cells[excelRow, 14].Value = invoice.Country;

                // Aplicar bordes a toda la fila
                for (int col = 1; col <= headers.Length; col++)
                {
                    worksheet.Cells[excelRow, col].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }
            }

            // Ajustar ancho de columnas
            worksheet.Cells.AutoFitColumns();

            // Asegurar que las columnas de números tengan ancho mínimo
            worksheet.Column(4).Width = Math.Max(worksheet.Column(4).Width, 15);
            worksheet.Column(5).Width = Math.Max(worksheet.Column(5).Width, 15);
            worksheet.Column(6).Width = Math.Max(worksheet.Column(6).Width, 15);

            _logger.LogInformation("Excel exportado con {Count} facturas", invoices.Count);

            return package.GetAsByteArray();
        }, cancellationToken);
    }
}

