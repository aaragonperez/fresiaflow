using FresiaFlow.Application.Ports.Inbound;
using FresiaFlow.Application.Ports.Outbound;
using FresiaFlow.Domain.InvoicesReceived;
using FresiaFlow.Domain.Shared;
using Microsoft.Extensions.Logging;

namespace FresiaFlow.Application.UseCases;

/// <summary>
/// Caso de uso para actualizar cualquier campo de una factura recibida.
/// </summary>
public class UpdateInvoiceUseCase : IUpdateInvoiceUseCase
{
    private readonly IInvoiceReceivedRepository _repository;
    private readonly ILogger<UpdateInvoiceUseCase> _logger;

    public UpdateInvoiceUseCase(
        IInvoiceReceivedRepository repository,
        ILogger<UpdateInvoiceUseCase> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task ExecuteAsync(UpdateInvoiceCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Actualizando factura {InvoiceId}", command.InvoiceId);

        var invoice = await _repository.GetByIdAsync(command.InvoiceId, cancellationToken);
        
        if (invoice == null)
        {
            throw new InvalidOperationException($"No se encontró la factura con ID {command.InvoiceId}");
        }

        // Actualizar solo los campos proporcionados
        if (command.InvoiceNumber != null)
        {
            invoice.SetInvoiceNumber(command.InvoiceNumber);
            _logger.LogDebug("Actualizado InvoiceNumber a {InvoiceNumber}", command.InvoiceNumber);
        }

        if (command.SupplierName != null)
        {
            invoice.SetSupplierName(command.SupplierName);
            _logger.LogDebug("Actualizado SupplierName a {SupplierName}", command.SupplierName);
        }

        if (command.SupplierTaxId != null)
        {
            invoice.SetSupplierTaxId(command.SupplierTaxId);
            _logger.LogDebug("Actualizado SupplierTaxId a {SupplierTaxId}", command.SupplierTaxId);
        }

        if (command.IssueDate.HasValue)
        {
            invoice.SetIssueDate(command.IssueDate.Value);
            _logger.LogDebug("Actualizado IssueDate a {IssueDate}", command.IssueDate);
        }

        // DueDate ya no existe en el modelo contable - se eliminó
        // Las facturas recibidas solo tienen fecha de emisión y fecha de recepción

        if (command.TotalAmount.HasValue)
        {
            var currency = command.Currency ?? invoice.Currency;
            invoice.SetTotalAmount(new Money(command.TotalAmount.Value, currency));
            _logger.LogDebug("Actualizado TotalAmount a {TotalAmount} {Currency}", command.TotalAmount, currency);
        }

        if (command.TaxAmount.HasValue)
        {
            var currency = command.Currency ?? invoice.Currency;
            invoice.SetTaxAmount(new Money(command.TaxAmount.Value, currency));
            _logger.LogDebug("Actualizado TaxAmount a {TaxAmount} {Currency}", command.TaxAmount, currency);
        }

        if (command.SubtotalAmount.HasValue)
        {
            var currency = command.Currency ?? invoice.Currency;
            invoice.SetSubtotalAmount(new Money(command.SubtotalAmount.Value, currency));
            _logger.LogDebug("Actualizado SubtotalAmount a {SubtotalAmount} {Currency}", command.SubtotalAmount, currency);
        }

        if (command.Currency != null && !command.TotalAmount.HasValue && !command.TaxAmount.HasValue && !command.SubtotalAmount.HasValue)
        {
            invoice.SetCurrency(command.Currency);
            _logger.LogDebug("Actualizado Currency a {Currency}", command.Currency);
        }

        if (command.Notes != null)
        {
            invoice.SetNotes(command.Notes);
            _logger.LogDebug("Actualizado Notes");
        }
        
        await _repository.UpdateAsync(invoice, cancellationToken);
        
        _logger.LogInformation("Factura {InvoiceId} actualizada correctamente", command.InvoiceId);
    }
}

