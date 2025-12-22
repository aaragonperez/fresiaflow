namespace FresiaFlow.Application.Ports.Inbound;

/// <summary>
/// Puerto de entrada para importar facturas emitidas desde Excel.
/// </summary>
public interface IImportIssuedInvoicesFromExcelUseCase
{
    Task<ImportIssuedInvoicesResult> ExecuteAsync(
        ImportIssuedInvoicesFromExcelCommand command,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Comando para importar facturas desde Excel.
/// </summary>
public record ImportIssuedInvoicesFromExcelCommand(
    Stream FileStream,
    string FileName,
    string ContentType
);

/// <summary>
/// Resultado de la importación de facturas.
/// </summary>
public record ImportIssuedInvoicesResult(
    int ImportedCount,
    int DuplicatesCount,
    int ErrorsCount,
    string Message
);

