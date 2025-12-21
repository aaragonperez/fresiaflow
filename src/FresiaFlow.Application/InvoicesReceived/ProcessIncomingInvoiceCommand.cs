namespace FresiaFlow.Application.InvoicesReceived;

/// <summary>
/// Comando para procesar una factura entrante desde archivo PDF.
/// </summary>
public record ProcessIncomingInvoiceCommand(string FilePath);

