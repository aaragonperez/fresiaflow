using FresiaFlow.Application.InvoicesReceived;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FresiaFlow.Infrastructure.HostedServices;

/// <summary>
/// Servicio en segundo plano que monitoriza una carpeta para procesar facturas automáticamente.
/// </summary>
public class FileSystemInvoiceWatcherService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IncomingInvoiceOptions _options;
    private readonly ILogger<FileSystemInvoiceWatcherService> _logger;
    private FileSystemWatcher? _watcher;

    public FileSystemInvoiceWatcherService(
        IServiceScopeFactory serviceScopeFactory,
        IOptions<IncomingInvoiceOptions> options,
        ILogger<FileSystemInvoiceWatcherService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Iniciando monitoreo de facturas en carpeta: {WatchFolder}",
            _options.WatchFolder);

        // Crear carpetas si no existen
        EnsureDirectoriesExist();

        // Configurar FileSystemWatcher
        _watcher = new FileSystemWatcher(_options.WatchFolder)
        {
            Filter = "*.pdf",
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
            EnableRaisingEvents = true
        };

        _watcher.Created += OnFileCreated;
        _watcher.Error += OnWatcherError;

        _logger.LogInformation("FileSystemWatcher configurado y activo");

        // Procesar archivos existentes al inicio
        await ProcessExistingFilesAsync(stoppingToken);

        // Mantener el servicio activo con escaneo periódico de respaldo
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(
                TimeSpan.FromSeconds(_options.ScanIntervalSeconds),
                stoppingToken);

            await ProcessExistingFilesAsync(stoppingToken);
        }
    }

    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        _logger.LogInformation("Nuevo archivo detectado: {FilePath}", e.FullPath);

        // Procesar de forma asíncrona sin bloquear el watcher
        _ = Task.Run(async () =>
        {
            // Esperar un poco para asegurar que el archivo se haya terminado de copiar
            await Task.Delay(1000);
            await ProcessFileAsync(e.FullPath, CancellationToken.None);
        });
    }

    private void OnWatcherError(object sender, ErrorEventArgs e)
    {
        _logger.LogError(e.GetException(), "Error en FileSystemWatcher");
    }

    private async Task ProcessExistingFilesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var files = Directory.GetFiles(_options.WatchFolder, "*.pdf");

            if (files.Length > 0)
            {
                _logger.LogInformation("Encontrados {Count} archivos PDF para procesar", files.Length);

                foreach (var file in files)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    await ProcessFileAsync(file, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando archivos existentes");
        }
    }

    private async Task ProcessFileAsync(string filePath, CancellationToken cancellationToken)
    {
        var fileName = Path.GetFileName(filePath);
        _logger.LogInformation("Procesando archivo: {FileName}", fileName);

        try
        {
            // Verificar que el archivo existe y no está en uso
            if (!IsFileReady(filePath))
            {
                _logger.LogWarning("El archivo {FileName} no está listo, se reintentará después", fileName);
                return;
            }

            // Crear un scope para resolver servicios Scoped
            using var scope = _serviceScopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<ProcessIncomingInvoiceCommandHandler>();

            // Procesar la factura
            var command = new ProcessIncomingInvoiceCommand(filePath);
            var invoiceId = await handler.HandleAsync(command, cancellationToken);

            // Mover a carpeta de éxito
            var successPath = Path.Combine(
                _options.ProcessedSuccessFolder,
                $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{fileName}");

            File.Move(filePath, successPath);

            _logger.LogInformation(
                "Factura {FileName} procesada exitosamente con ID {InvoiceId} y movida a {SuccessPath}",
                fileName,
                invoiceId,
                successPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando factura {FileName}", fileName);

            try
            {
                // Mover a carpeta de error
                var errorPath = Path.Combine(
                    _options.ProcessedErrorFolder,
                    $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_ERROR_{fileName}");

                File.Move(filePath, errorPath);

                // Crear archivo de log con el error
                var errorLogPath = Path.ChangeExtension(errorPath, ".error.txt");
                await File.WriteAllTextAsync(
                    errorLogPath,
                    $"Error: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}",
                    cancellationToken);

                _logger.LogInformation(
                    "Archivo con error movido a {ErrorPath}",
                    errorPath);
            }
            catch (Exception moveEx)
            {
                _logger.LogError(
                    moveEx,
                    "Error adicional al mover archivo con error {FileName}",
                    fileName);
            }
        }
    }

    private bool IsFileReady(string filePath)
    {
        try
        {
            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
    }

    private void EnsureDirectoriesExist()
    {
        Directory.CreateDirectory(_options.WatchFolder);
        Directory.CreateDirectory(_options.ProcessedSuccessFolder);
        Directory.CreateDirectory(_options.ProcessedErrorFolder);

        _logger.LogDebug("Carpetas de trabajo creadas/verificadas");
    }

    public override void Dispose()
    {
        _watcher?.Dispose();
        base.Dispose();
    }
}

