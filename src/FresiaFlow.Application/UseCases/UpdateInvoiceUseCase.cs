using System.Linq;
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

        var invoicesToUpdate = new Dictionary<Guid, InvoiceReceived>
        {
            [invoice.Id] = invoice
        };

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

        if (command.SupplierAddress != null)
        {
            invoice.SetSupplierAddress(command.SupplierAddress);
            _logger.LogDebug("Actualizado SupplierAddress");
        }

        if (command.IssueDate.HasValue)
        {
            invoice.SetIssueDate(command.IssueDate.Value);
            _logger.LogDebug("Actualizado IssueDate a {IssueDate}", command.IssueDate);
        }

        if (command.ReceivedDate.HasValue)
        {
            invoice.SetReceivedDate(command.ReceivedDate.Value);
            _logger.LogDebug("Actualizado ReceivedDate a {ReceivedDate}", command.ReceivedDate);
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

        if (command.TaxRate.HasValue)
        {
            invoice.SetTaxRate(command.TaxRate);
            _logger.LogDebug("Actualizado TaxRate a {TaxRate}", command.TaxRate);
        }

        if (command.IrpfAmount.HasValue)
        {
            var currency = command.Currency ?? invoice.Currency;
            invoice.SetIrpfAmount(new Money(command.IrpfAmount.Value, currency));
            _logger.LogDebug("Actualizado IrpfAmount a {IrpfAmount} {Currency}", command.IrpfAmount, currency);
        }

        if (command.IrpfRate.HasValue)
        {
            invoice.SetIrpfRate(command.IrpfRate);
            _logger.LogDebug("Actualizado IrpfRate a {IrpfRate}", command.IrpfRate);
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

        if (command.Lines != null)
        {
            var currencyForLines = command.Currency ?? invoice.Currency;
            var newLines = command.Lines.Select(lineDto =>
            {
                var unitPriceCurrency = lineDto.UnitPriceCurrency ?? currencyForLines;
                var lineTotalCurrency = lineDto.LineTotalCurrency ?? currencyForLines;

                var line = new InvoiceReceivedLine(
                    lineDto.LineNumber,
                    lineDto.Description,
                    lineDto.Quantity,
                    new Money(lineDto.UnitPrice, unitPriceCurrency),
                    new Money(lineDto.LineTotal, lineTotalCurrency));

                line.SetTaxRate(lineDto.TaxRate);
                return line;
            }).ToList();

            invoice.ReplaceLines(newLines);
            _logger.LogDebug("Reemplazadas {LineCount} líneas de detalle", newLines.Count);
        }

        // Propagar CIF/dirección a todas las facturas del mismo proveedor
        if (!string.IsNullOrWhiteSpace(command.SupplierTaxId))
        {
            var sameSupplierInvoices = await _repository.GetByExactSupplierNameAsync(invoice.SupplierName, cancellationToken);
            foreach (var other in sameSupplierInvoices)
            {
                if (other.Id == invoice.Id)
                    continue;

                other.SetSupplierTaxId(invoice.SupplierTaxId);

                if (command.SupplierAddress != null && string.IsNullOrWhiteSpace(other.SupplierAddress))
                {
                    other.SetSupplierAddress(command.SupplierAddress);
                }

                invoicesToUpdate[other.Id] = other;
            }

            if (invoicesToUpdate.Count > 1)
            {
                _logger.LogInformation("Propagado CIF a {Count} facturas adicionales del proveedor {SupplierName}", invoicesToUpdate.Count - 1, invoice.SupplierName);
            }
        }

        await _repository.UpdateManyAsync(invoicesToUpdate.Values, cancellationToken);

        _logger.LogInformation("Factura {InvoiceId} actualizada correctamente", command.InvoiceId);
    }
}

