using FresiaFlow.Application.Ports.Outbound;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;

namespace FresiaFlow.Adapters.Outbound.Excel;

/// <summary>
/// Servicio para procesar archivos Excel con facturas emitidas.
/// </summary>
public class ExcelProcessorService : IExcelProcessor
{
    private readonly ILogger<ExcelProcessorService> _logger;

    public ExcelProcessorService(ILogger<ExcelProcessorService> logger)
    {
        _logger = logger;
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    public async Task<List<IssuedInvoiceData>> ProcessExcelAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"El archivo Excel no existe: {filePath}");
        }

        _logger.LogInformation("Procesando archivo Excel: {FilePath}", filePath);

        return await Task.Run(() =>
        {
            var invoices = new List<IssuedInvoiceData>();

            try
            {
                using var package = new ExcelPackage(new FileInfo(filePath));
                var worksheet = package.Workbook.Worksheets[0]; // Primera hoja

                if (worksheet == null)
                {
                    throw new InvalidOperationException("El archivo Excel no contiene hojas.");
                }

                // Buscar la fila de encabezados
                int headerRow = FindHeaderRow(worksheet);
                if (headerRow == 0)
                {
                    throw new InvalidOperationException("No se encontraron encabezados válidos en el Excel.");
                }

                _logger.LogDebug("Encabezados encontrados en la fila {Row}", headerRow);

                // Mapear columnas
                var columnMap = MapColumns(worksheet, headerRow);

                // Procesar filas de datos
                int row = headerRow + 1;
                while (row <= worksheet.Dimension.End.Row)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    var invoice = ExtractInvoiceFromRow(worksheet, row, columnMap);
                    if (invoice != null)
                    {
                        invoices.Add(invoice);
                    }
                    row++;
                }

                _logger.LogInformation("Procesadas {Count} facturas del archivo Excel", invoices.Count);
                return invoices;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando archivo Excel: {FilePath}", filePath);
                throw new InvalidOperationException($"Error al procesar el archivo Excel: {ex.Message}", ex);
            }
        }, cancellationToken);
    }

    private int FindHeaderRow(ExcelWorksheet worksheet)
    {
        // Buscar fila con encabezados comunes
        for (int row = 1; row <= Math.Min(10, worksheet.Dimension?.End.Row ?? 10); row++)
        {
            var cellValue = worksheet.Cells[row, 1]?.Value?.ToString()?.ToUpper() ?? "";
            if (cellValue.Contains("SERIE") || cellValue.Contains("FECHA") || cellValue.Contains("FACTURA"))
            {
                return row;
            }
        }
        return 0;
    }

    private Dictionary<string, int> MapColumns(ExcelWorksheet worksheet, int headerRow)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        
        for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
        {
            var header = worksheet.Cells[headerRow, col]?.Value?.ToString()?.Trim() ?? "";
            
            if (string.IsNullOrWhiteSpace(header))
                continue;

            // Mapear según el formato esperado
            if (header.Contains("SERIE", StringComparison.OrdinalIgnoreCase))
                map["SERIE"] = col;
            else if (header.Contains("FECHA") && header.Contains("FACTURA", StringComparison.OrdinalIgnoreCase))
                map["FECHA FACTURA"] = col;
            else if (header.Contains("Nº") || header.Contains("NUMERO") || header.Contains("NÚMERO", StringComparison.OrdinalIgnoreCase))
                map["Nº FACTURA"] = col;
            else if (header.Contains("BASE IMPONIBLE", StringComparison.OrdinalIgnoreCase))
                map["BASE IMPONIBLE"] = col;
            else if (header.Contains("IVA", StringComparison.OrdinalIgnoreCase) && !header.Contains("TOTAL"))
                map["IVA"] = col;
            else if (header.Contains("TOTAL", StringComparison.OrdinalIgnoreCase))
                map["TOTAL FACTURAS"] = col;
            else if (header.Contains("ID CLIENTE", StringComparison.OrdinalIgnoreCase))
                map["ID CLIENTE"] = col;
            else if (header.Contains("NIF", StringComparison.OrdinalIgnoreCase))
                map["NIF"] = col;
            else if (header.Contains("NOMBRE") && header.Contains("CLIENTE", StringComparison.OrdinalIgnoreCase))
                map["NOMBRE CLIENTE"] = col;
            else if (header.Contains("DOMICILIO", StringComparison.OrdinalIgnoreCase))
                map["DOMICILIO"] = col;
            else if (header.Contains("LOCALIDAD", StringComparison.OrdinalIgnoreCase))
                map["LOCALIDAD"] = col;
            else if (header.Contains("COD") && header.Contains("POSTAL", StringComparison.OrdinalIgnoreCase))
                map["COD. POSTAL"] = col;
            else if (header.Contains("PROVINCIA", StringComparison.OrdinalIgnoreCase))
                map["PROVINCIA"] = col;
            else if (header.Contains("PAÍS", StringComparison.OrdinalIgnoreCase) || header.Contains("PAIS", StringComparison.OrdinalIgnoreCase))
                map["PAÍS"] = col;
        }

        _logger.LogDebug("Columnas mapeadas: {Count}", map.Count);
        return map;
    }

    private IssuedInvoiceData? ExtractInvoiceFromRow(ExcelWorksheet worksheet, int row, Dictionary<string, int> columnMap)
    {
        try
        {
            // Validar que hay número de factura (campo mínimo requerido)
            if (!columnMap.ContainsKey("Nº FACTURA"))
                return null;

            var invoiceNumber = GetCellValue(worksheet, row, columnMap["Nº FACTURA"]);
            if (string.IsNullOrWhiteSpace(invoiceNumber))
                return null; // Fila vacía

            var series = GetCellValue(worksheet, row, columnMap.GetValueOrDefault("SERIE", 0));
            var issueDateStr = GetCellValue(worksheet, row, columnMap.GetValueOrDefault("FECHA FACTURA", 0));
            var taxableBaseStr = GetCellValue(worksheet, row, columnMap.GetValueOrDefault("BASE IMPONIBLE", 0));
            var taxAmountStr = GetCellValue(worksheet, row, columnMap.GetValueOrDefault("IVA", 0));
            var totalAmountStr = GetCellValue(worksheet, row, columnMap.GetValueOrDefault("TOTAL FACTURAS", 0));
            var clientId = GetCellValue(worksheet, row, columnMap.GetValueOrDefault("ID CLIENTE", 0));
            var clientTaxId = GetCellValue(worksheet, row, columnMap.GetValueOrDefault("NIF", 0));
            var clientName = GetCellValue(worksheet, row, columnMap.GetValueOrDefault("NOMBRE CLIENTE", 0));
            var address = GetCellValue(worksheet, row, columnMap.GetValueOrDefault("DOMICILIO", 0));
            var city = GetCellValue(worksheet, row, columnMap.GetValueOrDefault("LOCALIDAD", 0));
            var postalCode = GetCellValue(worksheet, row, columnMap.GetValueOrDefault("COD. POSTAL", 0));
            var province = GetCellValue(worksheet, row, columnMap.GetValueOrDefault("PROVINCIA", 0));
            var country = GetCellValue(worksheet, row, columnMap.GetValueOrDefault("PAÍS", 0));

            // Parsear fecha
            DateTime issueDate;
            if (!DateTime.TryParse(issueDateStr, out issueDate))
            {
                _logger.LogWarning("Fecha inválida en fila {Row}: {DateStr}", row, issueDateStr);
                issueDate = DateTime.Today;
            }

            // Parsear decimales
            decimal.TryParse(taxableBaseStr?.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var taxableBase);
            decimal.TryParse(taxAmountStr?.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var taxAmount);
            decimal.TryParse(totalAmountStr?.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var totalAmount);

            return new IssuedInvoiceData(
                series ?? "",
                issueDate,
                invoiceNumber,
                taxableBase,
                taxAmount,
                totalAmount,
                clientId ?? "",
                clientTaxId ?? "",
                clientName ?? "",
                address ?? "",
                city ?? "",
                postalCode ?? "",
                province ?? "",
                country ?? "ES"
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extrayendo factura de la fila {Row}", row);
            return null;
        }
    }

    private string? GetCellValue(ExcelWorksheet worksheet, int row, int col)
    {
        if (col == 0)
            return null;

        var cell = worksheet.Cells[row, col];
        if (cell?.Value == null)
            return null;

        return cell.Value.ToString()?.Trim();
    }
}

