namespace FresiaFlow.Application.Ports.Inbound;

/// <summary>
/// Puerto de entrada para exportar facturas emitidas.
/// </summary>
public interface IExportIssuedInvoicesUseCase
{
    Task<ExportIssuedInvoicesResult> ExecuteAsync(
        ExportIssuedInvoicesCommand command,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Comando para exportar facturas emitidas.
/// </summary>
public record ExportIssuedInvoicesCommand(
    DateTime? StartDate,
    DateTime? EndDate,
    int? Year,
    int? Quarter, // 1-4
    int? Month // 1-12
);

/// <summary>
/// Resultado de la exportación.
/// </summary>
public record ExportIssuedInvoicesResult(
    byte[] ExcelContent,
    string FileName,
    int InvoiceCount
);

