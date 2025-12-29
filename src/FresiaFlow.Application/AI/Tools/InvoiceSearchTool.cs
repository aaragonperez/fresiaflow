using FresiaFlow.Application.Ports.Inbound;
using System.Text.Json;

namespace FresiaFlow.Application.AI.Tools;

/// <summary>
/// Herramienta para buscar facturas por texto libre.
/// Busca en número de factura, proveedor, NIF/CIF, etc.
/// </summary>
public class InvoiceSearchTool
{
    private readonly IGetAllInvoicesUseCase _getAllInvoicesUseCase;

    public InvoiceSearchTool(IGetAllInvoicesUseCase getAllInvoicesUseCase)
    {
        _getAllInvoicesUseCase = getAllInvoicesUseCase;
    }

    /// <summary>
    /// Ejecuta la búsqueda de facturas por texto libre.
    /// </summary>
    public async Task<string> ExecuteAsync(string argumentsJson, CancellationToken cancellationToken = default)
    {
        try
        {
            // Parsear argumentos JSON
            var args = JsonSerializer.Deserialize<SearchArguments>(argumentsJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (args == null || string.IsNullOrWhiteSpace(args.Query))
            {
                return JsonSerializer.Serialize(new
                {
                    error = "La consulta de búsqueda no puede estar vacía",
                    action = (object?)null
                });
            }

            // Obtener todas las facturas
            var allInvoices = await _getAllInvoicesUseCase.ExecuteAsync(cancellationToken);

            // Filtrar por texto libre (búsqueda en múltiples campos)
            var queryLower = args.Query.ToLowerInvariant();
            var matchingInvoices = allInvoices.Where(invoice =>
                (invoice.InvoiceNumber != null && invoice.InvoiceNumber.ToLowerInvariant().Contains(queryLower)) ||
                (invoice.SupplierName != null && invoice.SupplierName.ToLowerInvariant().Contains(queryLower)) ||
                (invoice.SupplierTaxId != null && invoice.SupplierTaxId.ToLowerInvariant().Contains(queryLower)) ||
                (invoice.Notes != null && invoice.Notes.ToLowerInvariant().Contains(queryLower))
            ).ToList();

            var count = matchingInvoices.Count;

            // Construir respuesta
            var result = new
            {
                success = true,
                count = count,
                message = count > 0
                    ? $"Se encontraron {count} factura(s) que coinciden con '{args.Query}'."
                    : $"No se encontraron facturas que coincidan con '{args.Query}'.",
                invoices = matchingInvoices.Take(10).Select(i => new
                {
                    id = i.Id,
                    invoiceNumber = i.InvoiceNumber,
                    supplierName = i.SupplierName,
                    issueDate = i.IssueDate,
                    totalAmount = i.TotalAmount
                }).ToList(),
                action = (object?)null // La búsqueda no requiere acción en frontend, solo mostrar resultados
            };

            return JsonSerializer.Serialize(result);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                error = $"Error al buscar facturas: {ex.Message}",
                action = (object?)null
            });
        }
    }

    private class SearchArguments
    {
        public string Query { get; set; } = string.Empty;
    }
}

