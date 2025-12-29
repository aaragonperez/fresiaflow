using FresiaFlow.Application.Ports.Inbound;
using System.Text.Json;

namespace FresiaFlow.Application.AI.Tools;

/// <summary>
/// Herramienta para filtrar facturas recibidas.
/// Ejecuta el caso de uso de filtrado y retorna resultados con acción para el frontend.
/// </summary>
public class InvoiceFilterTool
{
    private readonly IGetFilteredInvoicesUseCase _getFilteredInvoicesUseCase;

    public InvoiceFilterTool(IGetFilteredInvoicesUseCase getFilteredInvoicesUseCase)
    {
        _getFilteredInvoicesUseCase = getFilteredInvoicesUseCase;
    }

    /// <summary>
    /// Ejecuta el filtrado de facturas y retorna resultados con acción para el frontend.
    /// </summary>
    public async Task<string> ExecuteAsync(string argumentsJson, CancellationToken cancellationToken = default)
    {
        try
        {
            // Parsear argumentos JSON
            var args = JsonSerializer.Deserialize<FilterArguments>(argumentsJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (args == null)
            {
                return JsonSerializer.Serialize(new
                {
                    error = "Argumentos inválidos",
                    action = (object?)null
                });
            }

            // Convertir paymentType string a enum
            Domain.InvoicesReceived.PaymentType? paymentTypeEnum = null;
            if (!string.IsNullOrWhiteSpace(args.PaymentType))
            {
                if (Enum.TryParse<Domain.InvoicesReceived.PaymentType>(args.PaymentType, true, out var parsed))
                {
                    paymentTypeEnum = parsed;
                }
            }

            // Ejecutar caso de uso
            var invoices = await _getFilteredInvoicesUseCase.ExecuteAsync(
                args.Year,
                args.Quarter,
                args.SupplierName,
                paymentTypeEnum,
                cancellationToken);

            var invoiceList = invoices.ToList();
            var count = invoiceList.Count;

            // Construir respuesta con resultados y acción
            var result = new
            {
                success = true,
                count = count,
                message = count > 0
                    ? $"Se encontraron {count} factura(s) con los filtros aplicados."
                    : "No se encontraron facturas con los filtros aplicados.",
                action = new
                {
                    type = "applyInvoiceFilter",
                    @params = new
                    {
                        year = args.Year,
                        quarter = args.Quarter,
                        supplierName = args.SupplierName,
                        paymentType = args.PaymentType
                    }
                },
                summary = new
                {
                    year = args.Year,
                    quarter = args.Quarter,
                    supplierName = args.SupplierName,
                    paymentType = args.PaymentType
                }
            };

            return JsonSerializer.Serialize(result);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                error = $"Error al filtrar facturas: {ex.Message}",
                action = (object?)null
            });
        }
    }

    private class FilterArguments
    {
        public int? Year { get; set; }
        public int? Quarter { get; set; }
        public string? SupplierName { get; set; }
        public string? PaymentType { get; set; }
    }
}

