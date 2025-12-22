namespace FresiaFlow.Application.Ports.Inbound;

/// <summary>
/// Puerto de entrada para actualizar el proveedor de una factura.
/// </summary>
public interface IUpdateInvoiceSupplierUseCase
{
    Task ExecuteAsync(UpdateInvoiceSupplierCommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// Comando para actualizar el proveedor de una factura.
/// </summary>
public record UpdateInvoiceSupplierCommand(
    Guid InvoiceId,
    string SupplierName
);

