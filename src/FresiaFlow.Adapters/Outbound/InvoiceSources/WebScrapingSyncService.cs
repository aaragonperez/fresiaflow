using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FresiaFlow.Adapters.Outbound.Persistence;
using FresiaFlow.Application.Ports.Inbound;
using FresiaFlow.Application.Ports.Outbound;
using FresiaFlow.Domain.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using HtmlAgilityPack;

namespace FresiaFlow.Adapters.Outbound.InvoiceSources;

/// <summary>
/// Servicio de sincronización de facturas mediante web scraping genérico.
/// Usa HttpClient y HtmlAgilityPack para extraer enlaces a facturas de sitios web.
/// </summary>
public class WebScrapingSyncService : IInvoiceSourceSyncService
{
    private readonly FresiaFlowDbContext _dbContext;
    private readonly IUploadInvoiceUseCase _uploadInvoiceUseCase;
    private readonly ILogger<WebScrapingSyncService> _logger;
    private readonly ISyncProgressNotifier _progressNotifier;
    private readonly IHttpClientFactory _httpClientFactory;

    private static readonly string[] SupportedExtensions = { ".pdf", ".jpg", ".jpeg", ".png", ".gif", ".webp" };

    public WebScrapingSyncService(
        FresiaFlowDbContext dbContext,
        IUploadInvoiceUseCase uploadInvoiceUseCase,
        ILogger<WebScrapingSyncService> logger,
        ISyncProgressNotifier progressNotifier,
        IHttpClientFactory httpClientFactory)
    {
        _dbContext = dbContext;
        _uploadInvoiceUseCase = uploadInvoiceUseCase;
        _logger = logger;
        _progressNotifier = progressNotifier;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<InvoiceSourceConfig?> GetConfigAsync(Guid sourceId)
    {
        return await _dbContext.InvoiceSourceConfigs
            .FirstOrDefaultAsync(c => c.Id == sourceId && c.SourceType == InvoiceSourceType.WebScraping);
    }

    public async Task<InvoiceSourceConfig> SaveConfigAsync(InvoiceSourceConfig config)
    {
        if (config.SourceType != InvoiceSourceType.WebScraping)
        {
            throw new ArgumentException("El tipo de fuente debe ser WebScraping", nameof(config));
        }

        var existing = await _dbContext.InvoiceSourceConfigs.FindAsync(config.Id);
        if (existing == null)
        {
            _dbContext.InvoiceSourceConfigs.Add(config);
        }
        else
        {
            existing.UpdateConfig(config.Name, config.ConfigJson);
            if (config.Enabled)
                existing.Enable();
            else
                existing.Disable();
        }

        await _dbContext.SaveChangesAsync();
        return config;
    }

    public async Task<SyncPreview> GetSyncPreviewAsync(Guid sourceId, CancellationToken cancellationToken = default)
    {
        var preview = new SyncPreview();

        try
        {
            var config = await GetConfigAsync(sourceId);
            if (config == null || !config.Enabled)
            {
                preview.ErrorMessage = "Configuración de web scraping no encontrada o deshabilitada";
                return preview;
            }

            var scrapingConfig = DeserializeScrapingConfig(config.ConfigJson);
            if (scrapingConfig == null)
            {
                preview.ErrorMessage = "Configuración de web scraping inválida";
                return preview;
            }

            // Obtener enlaces a facturas
            var invoiceLinks = await FindInvoiceLinksAsync(scrapingConfig, cancellationToken);
            preview.TotalFiles = invoiceLinks.Count;
            preview.SupportedFiles = invoiceLinks.Count; // Todos los enlaces encontrados son soportados

            // Verificar cuántos ya están sincronizados
            var urls = invoiceLinks.Select(l => l.Url).ToList();
            var syncedCount = await _dbContext.SyncedFiles
                .Where(s => s.Source == $"WebScraping-{sourceId}" && urls.Contains(s.ExternalId))
                .CountAsync(cancellationToken);

            preview.AlreadySynced = syncedCount;
            preview.PendingToProcess = preview.SupportedFiles - preview.AlreadySynced;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo preview de web scraping");
            preview.ErrorMessage = ex.Message;
        }

        return preview;
    }

    public async Task<SyncResult> SyncNowAsync(Guid sourceId, bool forceReprocess = false, CancellationToken cancellationToken = default)
    {
        var result = new SyncResult();
        var config = await GetConfigAsync(sourceId);

        if (config == null || !config.Enabled)
        {
            result.Success = false;
            result.ErrorMessage = "Configuración de web scraping no encontrada o deshabilitada";
            return result;
        }

        var scrapingConfig = DeserializeScrapingConfig(config.ConfigJson);
        if (scrapingConfig == null)
        {
            result.Success = false;
            result.ErrorMessage = "Configuración de web scraping inválida";
            return result;
        }

        try
        {
            _logger.LogInformation("Iniciando web scraping: {SourceName}", config.Name);

            // Encontrar enlaces a facturas
            var invoiceLinks = await FindInvoiceLinksAsync(scrapingConfig, cancellationToken);
            var totalFiles = invoiceLinks.Count;
            var processedFiles = 0;

            await _progressNotifier.NotifyAsync(new SyncProgressUpdate
            {
                CurrentFile = "Iniciando web scraping...",
                ProcessedCount = 0,
                TotalCount = totalFiles,
                Percentage = 0,
                Status = "syncing",
                Message = $"Se encontraron {totalFiles} facturas"
            }, cancellationToken);

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromMinutes(5);

            foreach (var link in invoiceLinks)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    result.Success = false;
                    result.ErrorMessage = "Sincronización cancelada";
                    break;
                }

                try
                {
                    // Verificar si ya fue sincronizado
                    var existingSync = await _dbContext.SyncedFiles
                        .FirstOrDefaultAsync(s => s.Source == $"WebScraping-{sourceId}" && s.ExternalId == link.Url, cancellationToken);

                    if (existingSync != null)
                    {
                        // Si ya existe y está completado
                        if (existingSync.Status == SyncStatus.Completed)
                        {
                            // Si se fuerza el reproceso, reprocesar
                            if (forceReprocess)
                            {
                                _logger.LogInformation("Forzando reproceso de: {Url}", link.Url);
                                existingSync.MarkAsProcessing();
                            }
                            // Si NO se fuerza, saltar (ya procesado)
                            else
                            {
                                _logger.LogDebug("Saltando archivo ya procesado: {Url}", link.Url);
                                result.SkippedCount++;
                                processedFiles++;
                                continue;
                            }
                        }
                        // Si existe pero falló o está pendiente, reprocesar
                        else
                        {
                            _logger.LogInformation("Reprocesando archivo con estado {Status}: {Url}", existingSync.Status, link.Url);
                            existingSync.MarkAsProcessing();
                        }
                    }

                    // Descargar archivo
                    var fileData = await DownloadFileAsync(httpClient, link.Url, cancellationToken);
                    if (fileData == null)
                    {
                        if (existingSync != null)
                        {
                            existingSync.MarkAsFailed("No se pudo descargar el archivo");
                        }
                        result.FailedCount++;
                        result.DetailedErrors.Add($"{link.Url}: No se pudo descargar el archivo");
                        continue;
                    }

                    // Procesar factura
                    using var fileStream = new MemoryStream(fileData);
                    var fileName = link.FileName ?? Path.GetFileName(new Uri(link.Url).LocalPath) ?? "factura.pdf";
                    var uploadCommand = new UploadInvoiceCommand(
                        fileStream,
                        fileName,
                        GetContentType(fileName));

                    var invoiceResult = await _uploadInvoiceUseCase.ExecuteAsync(uploadCommand);

                    // Registrar como sincronizado
                    if (existingSync == null)
                    {
                        existingSync = SyncedFile.Create(
                            $"WebScraping-{sourceId}",
                            link.Url,
                            fileName,
                            link.Url,
                            fileData.Length,
                            ComputeHash(fileData),
                            DateTime.UtcNow
                        );
                        _dbContext.SyncedFiles.Add(existingSync);
                    }

                    existingSync.MarkAsCompleted(invoiceResult.InvoiceId);
                    result.ProcessedCount++;
                    processedFiles++;

                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    var syncFile = await _dbContext.SyncedFiles
                        .FirstOrDefaultAsync(s => s.Source == $"WebScraping-{sourceId}" && s.ExternalId == link.Url, cancellationToken);
                    
                    var errorMessage = ex.Message;
                    if (ex.InnerException != null)
                    {
                        errorMessage += $" -> {ex.InnerException.Message}";
                    }
                    
                    if (syncFile != null)
                    {
                        syncFile.MarkAsFailed(errorMessage);
                    }
                    
                    _logger.LogWarning(ex, "Error procesando enlace {Url}: {Error}", link.Url, errorMessage);
                    result.FailedCount++;
                    result.DetailedErrors.Add($"{link.Url}: {errorMessage}");
                }

                // Actualizar progreso
                var percentage = totalFiles > 0 ? (int)((double)processedFiles / totalFiles * 100) : 0;
                await _progressNotifier.NotifyAsync(new SyncProgressUpdate
                {
                    CurrentFile = link.FileName ?? link.Url,
                    ProcessedCount = processedFiles,
                    TotalCount = totalFiles,
                    Percentage = percentage,
                    Status = "syncing",
                    Message = $"Procesado: {link.FileName ?? link.Url}"
                }, cancellationToken);
            }

            config.RecordSuccessfulSync(result.ProcessedCount);
            result.Success = result.FailedCount == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante web scraping");
            config?.RecordFailedSync(ex.Message);
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }
        finally
        {
            await _dbContext.SaveChangesAsync();
        }

        return result;
    }

    public async Task<SourceValidationResult> ValidateConfigAsync(string configJson, CancellationToken cancellationToken = default)
    {
        try
        {
            var config = DeserializeScrapingConfig(configJson);
            if (config == null)
            {
                return new SourceValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Configuración JSON inválida"
                };
            }

            // Validar que la URL sea accesible
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            try
            {
                var response = await httpClient.GetAsync(config.Url, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    return new SourceValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"No se pudo acceder a la URL: {response.StatusCode}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new SourceValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"Error accediendo a la URL: {ex.Message}"
                };
            }

            return new SourceValidationResult
            {
                IsValid = true,
                Info = new Dictionary<string, object>
                {
                    { "url", config.Url },
                    { "hasSelectors", config.Selectors != null }
                }
            };
        }
        catch (Exception ex)
        {
            return new SourceValidationResult
            {
                IsValid = false,
                ErrorMessage = $"Error validando configuración: {ex.Message}"
            };
        }
    }

    private async Task<List<InvoiceLink>> FindInvoiceLinksAsync(WebScrapingConfig config, CancellationToken cancellationToken)
    {
        var links = new List<InvoiceLink>();
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromMinutes(5);

        // Agregar headers comunes para evitar bloqueos
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

        try
        {
            var html = await httpClient.GetStringAsync(config.Url, cancellationToken);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Buscar enlaces usando el selector configurado
            var selector = config.Selectors?.InvoiceLinks ?? "a[href*='factura'], a[href*='invoice'], a[href$='.pdf']";
            var linkNodes = doc.DocumentNode.SelectNodes(selector) ?? new HtmlNodeCollection(null);

            foreach (var node in linkNodes)
            {
                var href = node.GetAttributeValue("href", "");
                if (string.IsNullOrWhiteSpace(href))
                    continue;

                // Resolver URL relativa a absoluta
                var absoluteUrl = new Uri(new Uri(config.Url), href).ToString();

                // Verificar extensión soportada
                var ext = Path.GetExtension(absoluteUrl).ToLowerInvariant();
                if (!SupportedExtensions.Contains(ext))
                    continue;

                links.Add(new InvoiceLink
                {
                    Url = absoluteUrl,
                    FileName = node.InnerText.Trim() ?? Path.GetFileName(absoluteUrl)
                });
            }

            // Si hay selector de siguiente página, buscar más páginas
            if (!string.IsNullOrWhiteSpace(config.Selectors?.NextPage))
            {
                // TODO: Implementar paginación si es necesario
                _logger.LogInformation("Paginación no implementada aún");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error haciendo scraping de {Url}", config.Url);
            throw;
        }

        return links;
    }

    private async Task<byte[]?> DownloadFileAsync(HttpClient httpClient, string url, CancellationToken cancellationToken)
    {
        try
        {
            var response = await httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error descargando archivo {Url}", url);
            return null;
        }
    }

    private WebScrapingConfig? DeserializeScrapingConfig(string configJson)
    {
        try
        {
            return JsonSerializer.Deserialize<WebScrapingConfig>(configJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return null;
        }
    }

    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLower();
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }

    private static string ComputeHash(byte[] data)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(data);
        return Convert.ToBase64String(hash);
    }

    private class WebScrapingConfig
    {
        public string Url { get; set; } = string.Empty;
        public bool LoginRequired { get; set; }
        public string? LoginUrl { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public ScrapingSelectors? Selectors { get; set; }
    }

    private class ScrapingSelectors
    {
        public string InvoiceLinks { get; set; } = "a[href*='factura'], a[href*='invoice'], a[href$='.pdf']";
        public string? DownloadButton { get; set; }
        public string? NextPage { get; set; }
    }

    private class InvoiceLink
    {
        public string Url { get; set; } = string.Empty;
        public string? FileName { get; set; }
    }
}

