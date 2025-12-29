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
    DateTime? ReceivedDate = null,
    DateTime? DueDate = null,
    string? SupplierAddress = null,
    decimal? TotalAmount = null,
    decimal? TaxAmount = null,
    decimal? TaxRate = null,
    decimal? IrpfAmount = null,
    decimal? IrpfRate = null,
    decimal? SubtotalAmount = null,
    string? Currency = null,
    string? Notes = null,
    IEnumerable<UpdateInvoiceLineCommand>? Lines = null
);

/// <summary>
/// Comando para reemplazar líneas de detalle de una factura.
/// </summary>
public record UpdateInvoiceLineCommand(
    Guid? Id,
    int LineNumber,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    string? UnitPriceCurrency,
    decimal? TaxRate,
    decimal LineTotal,
    string? LineTotalCurrency
);

