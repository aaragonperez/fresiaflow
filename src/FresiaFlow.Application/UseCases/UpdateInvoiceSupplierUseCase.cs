using FresiaFlow.Application.Ports.Inbound;
using FresiaFlow.Application.Ports.Outbound;
using Microsoft.Extensions.Logging;

namespace FresiaFlow.Application.UseCases;

/// <summary>
/// Caso de uso para actualizar el nombre del proveedor de una factura recibida.
/// </summary>
public class UpdateInvoiceSupplierUseCase : IUpdateInvoiceSupplierUseCase
{
    private readonly IInvoiceReceivedRepository _repository;
    private readonly ILogger<UpdateInvoiceSupplierUseCase> _logger;

    public UpdateInvoiceSupplierUseCase(
        IInvoiceReceivedRepository repository,
        ILogger<UpdateInvoiceSupplierUseCase> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task ExecuteAsync(UpdateInvoiceSupplierCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Actualizando proveedor de factura {InvoiceId} a {SupplierName}", command.InvoiceId, command.SupplierName);

        var invoice = await _repository.GetByIdAsync(command.InvoiceId, cancellationToken);
        
        if (invoice == null)
        {
            throw new InvalidOperationException($"No se encontró la factura con ID {command.InvoiceId}");
        }

        invoice.SetSupplierName(command.SupplierName);
        
        await _repository.UpdateAsync(invoice, cancellationToken);
        
        _logger.LogInformation("Proveedor de factura {InvoiceId} actualizado correctamente a {SupplierName}", command.InvoiceId, command.SupplierName);
    }
}

