namespace FresiaFlow.Application.Ports.Inbound;

/// <summary>
/// Puerto de entrada para actualizar una factura recibida.
/// </summary>
public interface IUpdateInvoiceUseCase
{
    Task ExecuteAsync(UpdateInvoiceCommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// Comando para actualizar una factura recibida.
/// Todos los campos son opcionales, solo se actualizan los que se proporcionen.
/// </summary>
public record UpdateInvoiceCommand(
    Guid InvoiceId,
    string? InvoiceNumber = null,
    string? SupplierName = null,
    string? SupplierTaxId = null,
    DateTime? IssueDate = null,
    DateTime? DueDate = null,
    decimal? TotalAmount = null,
    decimal? TaxAmount = null,
    decimal? SubtotalAmount = null,
    string? Currency = null,
    string? Notes = null
);

