using System.Security.Cryptography;
using System.Text.Json;
using FresiaFlow.Adapters.Outbound.Persistence;
using FresiaFlow.Application.Ports.Inbound;
using FresiaFlow.Application.Ports.Outbound;
using FresiaFlow.Domain.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using Microsoft.Playwright;

namespace FresiaFlow.Adapters.Outbound.InvoiceSources;

/// <summary>
/// Servicio de sincronización de facturas desde portales web usando Playwright para automatización.
/// </summary>
public class PortalSyncService : IInvoiceSourceSyncService
{
    private readonly FresiaFlowDbContext _dbContext;
    private readonly IUploadInvoiceUseCase _uploadInvoiceUseCase;
    private readonly ILogger<PortalSyncService> _logger;
    private readonly ISyncProgressNotifier _progressNotifier;
    private readonly IHttpClientFactory _httpClientFactory;

    private static readonly string[] SupportedExtensions = { ".pdf", ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private const int NavigationTimeout = 120000; // 120 segundos (2 minutos) para portales lentos

    public PortalSyncService(
        FresiaFlowDbContext dbContext,
        IUploadInvoiceUseCase uploadInvoiceUseCase,
        ILogger<PortalSyncService> logger,
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
            .FirstOrDefaultAsync(c => c.Id == sourceId && c.SourceType == InvoiceSourceType.Portal);
    }

    public async Task<InvoiceSourceConfig> SaveConfigAsync(InvoiceSourceConfig config)
    {
        if (config.SourceType != InvoiceSourceType.Portal)
        {
            throw new ArgumentException("El tipo de fuente debe ser Portal", nameof(config));
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
                preview.ErrorMessage = "Configuración de portal no encontrada o deshabilitada";
                return preview;
            }

            var portalConfig = DeserializePortalConfig(config.ConfigJson);
            if (portalConfig == null)
            {
                preview.ErrorMessage = "Configuración de portal inválida";
                return preview;
            }

            // Usar Playwright para obtener preview
            var invoiceLinks = await FindInvoiceLinksAsync(portalConfig, cancellationToken);
            preview.TotalFiles = invoiceLinks.Count;
            preview.SupportedFiles = invoiceLinks.Count;

            // Verificar cuántos ya están sincronizados
            var urls = invoiceLinks.Select(l => l.Url).ToList();
            var syncedCount = await _dbContext.SyncedFiles
                .Where(s => s.Source == $"Portal-{sourceId}" && urls.Contains(s.ExternalId))
                .CountAsync(cancellationToken);

            preview.AlreadySynced = syncedCount;
            preview.PendingToProcess = preview.SupportedFiles - preview.AlreadySynced;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo preview de portal");
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
            result.ErrorMessage = "Configuración de portal no encontrada o deshabilitada";
            return result;
        }

        var portalConfig = DeserializePortalConfig(config.ConfigJson);
        if (portalConfig == null)
        {
            result.Success = false;
            result.ErrorMessage = "Configuración de portal inválida";
            return result;
        }

        try
        {
            _logger.LogInformation("Iniciando sincronización de portal: {SourceName}", config.Name);

            // Encontrar enlaces a facturas usando Playwright
            var invoiceLinks = await FindInvoiceLinksAsync(portalConfig, cancellationToken);
            var totalFiles = invoiceLinks.Count;
            var processedFiles = 0;

            await _progressNotifier.NotifyAsync(new SyncProgressUpdate
            {
                CurrentFile = "Iniciando sincronización de portal...",
                ProcessedCount = 0,
                TotalCount = totalFiles,
                Percentage = 0,
                Status = "syncing",
                Message = $"Se encontraron {totalFiles} facturas en el portal"
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
                        .FirstOrDefaultAsync(s => s.Source == $"Portal-{sourceId}" && s.ExternalId == link.Url, cancellationToken);

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

                    // Descargar archivo usando Playwright o HttpClient
                    byte[]? fileData;
                    if (link.RequiresBrowser)
                    {
                        fileData = await DownloadFileWithBrowserAsync(portalConfig, link, cancellationToken);
                    }
                    else
                    {
                        fileData = await DownloadFileAsync(httpClient, link.Url, cancellationToken);
                    }

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
                            $"Portal-{sourceId}",
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
                        .FirstOrDefaultAsync(s => s.Source == $"Portal-{sourceId}" && s.ExternalId == link.Url, cancellationToken);
                    
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
            _logger.LogError(ex, "Error durante sincronización de portal");
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
            var config = DeserializePortalConfig(configJson);
            if (config == null)
            {
                return new SourceValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Configuración JSON inválida"
                };
            }

            // Validar que BaseUrl no esté vacío
            if (string.IsNullOrWhiteSpace(config.BaseUrl))
            {
                return new SourceValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "El campo 'baseUrl' es obligatorio y no puede estar vacío"
                };
            }

            // Validar formato de URL
            if (!Uri.TryCreate(config.BaseUrl, UriKind.Absolute, out var baseUri))
            {
                return new SourceValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"La URL base '{config.BaseUrl}' no es una URL válida"
                };
            }

            // Validar que LoginUrl o InvoicesPageUrl sean válidas si están presentes
            if (!string.IsNullOrWhiteSpace(config.LoginUrl) && !Uri.TryCreate(config.LoginUrl, UriKind.Absolute, out _))
            {
                return new SourceValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"La URL de login '{config.LoginUrl}' no es una URL válida"
                };
            }

            if (!string.IsNullOrWhiteSpace(config.InvoicesPageUrl) && !Uri.TryCreate(config.InvoicesPageUrl, UriKind.Absolute, out _))
            {
                return new SourceValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"La URL de facturas '{config.InvoicesPageUrl}' no es una URL válida"
                };
            }

            // Intentar instalar Playwright si no está instalado
            try
            {
                using var playwright = await Playwright.CreateAsync();
                await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = true
                });

                var page = await ConfigurePageForFastLoading(browser);
                try
                {
                    var urlToTest = config.LoginUrl ?? config.InvoicesPageUrl ?? config.BaseUrl;
                    if (string.IsNullOrWhiteSpace(urlToTest))
                    {
                        return new SourceValidationResult
                        {
                            IsValid = false,
                            ErrorMessage = "Debe especificarse al menos 'baseUrl', 'loginUrl' o 'invoicesPageUrl'"
                        };
                    }
                    
                    await page.GotoAsync(urlToTest, new PageGotoOptions
                    {
                        WaitUntil = WaitUntilState.DOMContentLoaded, // Más rápido que Load
                        Timeout = NavigationTimeout
                    });

                    return new SourceValidationResult
                    {
                        IsValid = true,
                        Info = new Dictionary<string, object>
                        {
                            { "url", config.BaseUrl },
                            { "loginUrl", config.LoginUrl ?? "N/A" },
                            { "invoicesPageUrl", config.InvoicesPageUrl ?? "N/A" }
                        }
                    };
                }
                catch (Exception ex)
                {
                    return new SourceValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"Error accediendo al portal: {ex.Message}"
                    };
                }
            }
            catch (Exception playwrightEx)
            {
                // Si Playwright no está instalado, intentar instalarlo automáticamente
                if (playwrightEx.Message.Contains("Executable doesn't exist") || 
                    playwrightEx.Message.Contains("playwright.ps1 install") ||
                    playwrightEx.Message.Contains("chromium"))
                {
                    _logger.LogWarning("Playwright no está instalado. Intentando instalar automáticamente...");
                    try
                    {
                        // Intentar instalar usando npx si está disponible
                        var installProcess = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "npx",
                            Arguments = "playwright install chromium",
                            WorkingDirectory = Directory.GetCurrentDirectory(),
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        });
                        
                        if (installProcess != null)
                        {
                            await installProcess.WaitForExitAsync(cancellationToken);
                            if (installProcess.ExitCode == 0)
                            {
                                _logger.LogInformation("✅ Playwright instalado correctamente. Reintentando validación...");
                                // Esperar un momento para que los archivos se escriban completamente
                                await Task.Delay(1000, cancellationToken);
                                // Reintentar después de la instalación
                                return await ValidateConfigAsync(configJson, cancellationToken);
                            }
                        }
                    }
                    catch (Exception installEx)
                    {
                        _logger.LogWarning(installEx, "No se pudo instalar Playwright automáticamente");
                    }

                    return new SourceValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Playwright no está instalado. Ejecuta manualmente: npx playwright install chromium"
                    };
                }
                
                return new SourceValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"Error con Playwright: {playwrightEx.Message}"
                };
            }
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

    private async Task<List<PortalInvoiceLink>> FindInvoiceLinksAsync(PortalConfig config, CancellationToken cancellationToken)
    {
        var links = new List<PortalInvoiceLink>();

        // Validar que BaseUrl no esté vacío
        if (string.IsNullOrWhiteSpace(config.BaseUrl))
        {
            throw new ArgumentException("El campo 'baseUrl' es obligatorio y no puede estar vacío");
        }

        // Validar formato de URL
        if (!Uri.TryCreate(config.BaseUrl, UriKind.Absolute, out var baseUriForValidation))
        {
            throw new ArgumentException($"La URL base '{config.BaseUrl}' no es una URL válida");
        }

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });

        var page = await ConfigurePageForFastLoading(browser);

        try
        {
            // Navegar a la página de login si es necesario
            if (!string.IsNullOrEmpty(config.LoginUrl))
            {
                try
                {
                    await page.GotoAsync(config.LoginUrl, new PageGotoOptions
                    {
                        WaitUntil = WaitUntilState.DOMContentLoaded, // Más rápido que Load
                        Timeout = NavigationTimeout
                    });
                }
                catch (TimeoutException ex)
                {
                    _logger.LogWarning(ex, "Timeout navegando a login, intentando continuar...");
                    // Intentar continuar aunque haya timeout, la página puede estar suficientemente cargada
                }

                // Realizar login si hay credenciales
                if (!string.IsNullOrEmpty(config.Username) && !string.IsNullOrEmpty(config.Password))
                {
                    // Esperar un momento para que los campos estén disponibles
                    await Task.Delay(1000, cancellationToken);
                    
                    // Buscar campos de login usando selectores configurados o genéricos
                    var usernameSelector = config.Selectors?.UsernameField ?? "input[type='email'], input[name='username'], input[name='email'], input[id*='user'], input[id*='email']";
                    var passwordSelector = config.Selectors?.PasswordField ?? "input[type='password']";
                    var submitSelector = config.Selectors?.SubmitButton ?? "button[type='submit'], input[type='submit'], button:has-text('Iniciar'), button:has-text('Login'), button:has-text('Entrar')";

                    // Esperar a que los campos estén visibles antes de llenarlos
                    try
                    {
                        await page.WaitForSelectorAsync(usernameSelector, new PageWaitForSelectorOptions { Timeout = 30000 });
                        await page.WaitForSelectorAsync(passwordSelector, new PageWaitForSelectorOptions { Timeout = 30000 });
                    }
                    catch (TimeoutException)
                    {
                        _logger.LogWarning("No se encontraron los campos de login con los selectores predeterminados. Intentando continuar...");
                    }

                    await page.FillAsync(usernameSelector, config.Username);
                    await page.FillAsync(passwordSelector, config.Password);
                    await page.ClickAsync(submitSelector);
                    
                    // Esperar a que la página cargue después del login (usar DOMContentLoaded para ser más rápido)
                    try
                    {
                        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded, new PageWaitForLoadStateOptions { Timeout = NavigationTimeout });
                    }
                    catch (TimeoutException)
                    {
                        _logger.LogWarning("Timeout esperando carga después del login, continuando...");
                    }
                }
            }

            // Navegar a la página de facturas
            var invoicesUrl = config.InvoicesPageUrl ?? config.BaseUrl;
            if (string.IsNullOrWhiteSpace(invoicesUrl))
            {
                throw new ArgumentException("Debe especificarse 'invoicesPageUrl' o 'baseUrl'");
            }
            
            try
            {
                await page.GotoAsync(invoicesUrl, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded, // Más rápido que Load
                    Timeout = NavigationTimeout
                });
            }
            catch (TimeoutException ex)
            {
                _logger.LogWarning(ex, "Timeout navegando a página de facturas, intentando continuar...");
                // Intentar continuar aunque haya timeout
            }

            // Buscar enlaces a facturas usando el selector configurado
            var invoiceLinkSelector = config.Selectors?.InvoiceLinks ?? "a[href*='factura'], a[href*='invoice'], a[href$='.pdf'], a[download]";
            var linkElements = await page.QuerySelectorAllAsync(invoiceLinkSelector);

            foreach (var element in linkElements)
            {
                var href = await element.GetAttributeAsync("href");
                if (string.IsNullOrWhiteSpace(href))
                    continue;

                // Resolver URL relativa a absoluta
                string absoluteUrl;
                try
                {
                    if (Uri.IsWellFormedUriString(href, UriKind.Absolute))
                    {
                        absoluteUrl = href;
                    }
                    else
                    {
                        // Es una URL relativa, necesitamos BaseUrl
                        if (string.IsNullOrWhiteSpace(config.BaseUrl))
                        {
                            _logger.LogWarning("No se puede resolver URL relativa {Href} porque BaseUrl está vacío", href);
                            continue;
                        }
                        var baseUriForLink = new Uri(config.BaseUrl);
                        absoluteUrl = new Uri(baseUriForLink, href).ToString();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error construyendo URL absoluta para {Href}", href);
                    continue;
                }

                // Verificar extensión soportada
                var ext = Path.GetExtension(absoluteUrl).ToLowerInvariant();
                if (!SupportedExtensions.Contains(ext) && !absoluteUrl.Contains("factura", StringComparison.OrdinalIgnoreCase))
                    continue;

                var fileName = await element.TextContentAsync() ?? Path.GetFileName(absoluteUrl);
                links.Add(new PortalInvoiceLink
                {
                    Url = absoluteUrl,
                    FileName = fileName.Trim(),
                    RequiresBrowser = config.Selectors?.RequiresBrowserDownload == true
                });
            }

            // Si hay paginación, buscar más páginas
            if (!string.IsNullOrWhiteSpace(config.Selectors?.NextPage))
            {
                // TODO: Implementar paginación si es necesario
                _logger.LogInformation("Paginación no implementada aún");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error haciendo scraping de portal {Url}", config.BaseUrl);
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

    private async Task<byte[]?> DownloadFileWithBrowserAsync(PortalConfig config, PortalInvoiceLink link, CancellationToken cancellationToken)
    {
        try
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });

            var page = await ConfigurePageForFastLoading(browser);

            // Navegar a la URL del enlace
            try
            {
                await page.GotoAsync(link.Url, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded, // Más rápido que Load
                    Timeout = NavigationTimeout
                });
            }
            catch (TimeoutException ex)
            {
                _logger.LogWarning(ex, "Timeout navegando a {Url}, intentando continuar...", link.Url);
                // Intentar continuar aunque haya timeout
            }

            // Esperar a que se descargue el archivo o obtener el contenido
            var content = await page.ContentAsync();
            
            // Si es un PDF embebido, intentar obtenerlo
            // Por ahora, retornamos null y el sistema intentará con HttpClient
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error descargando archivo con navegador {Url}", link.Url);
            return null;
        }
    }

    private PortalConfig? DeserializePortalConfig(string configJson)
    {
        try
        {
            return JsonSerializer.Deserialize<PortalConfig>(configJson, new JsonSerializerOptions
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

    /// <summary>
    /// Configura una página de Playwright con bloqueo de recursos innecesarios y timeouts aumentados
    /// para acelerar la carga y evitar timeouts en portales lentos.
    /// </summary>
    private async Task<IPage> ConfigurePageForFastLoading(IBrowser browser)
    {
        var page = await browser.NewPageAsync();
        
        // Configurar timeout por defecto ANTES de cualquier operación
        page.SetDefaultTimeout(NavigationTimeout);
        page.SetDefaultNavigationTimeout(NavigationTimeout);

        // Bloquear recursos innecesarios para acelerar la carga
        // Esto evita que la página espere por recursos que no son necesarios para el login/scraping
        await page.RouteAsync("**/*", async route =>
        {
            var resourceType = route.Request.ResourceType;
            var url = route.Request.Url;

            // Bloquear recursos que no son necesarios para el funcionamiento básico
            if (resourceType == "image" || 
                resourceType == "stylesheet" || 
                resourceType == "font" ||
                resourceType == "media" ||
                url.Contains("analytics", StringComparison.OrdinalIgnoreCase) ||
                url.Contains("tracking", StringComparison.OrdinalIgnoreCase) ||
                url.Contains("advertising", StringComparison.OrdinalIgnoreCase) ||
                url.Contains("ads", StringComparison.OrdinalIgnoreCase) ||
                url.Contains("doubleclick", StringComparison.OrdinalIgnoreCase) ||
                url.Contains("google-analytics", StringComparison.OrdinalIgnoreCase) ||
                url.Contains("googletagmanager", StringComparison.OrdinalIgnoreCase) ||
                url.Contains("facebook.net", StringComparison.OrdinalIgnoreCase) ||
                url.Contains("twitter.com", StringComparison.OrdinalIgnoreCase))
            {
                await route.AbortAsync();
            }
            else
            {
                await route.ContinueAsync();
            }
        });

        return page;
    }

    private class PortalConfig
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string? LoginUrl { get; set; }
        public string? InvoicesPageUrl { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public PortalSelectors? Selectors { get; set; }
    }

    private class PortalSelectors
    {
        public string? UsernameField { get; set; }
        public string? PasswordField { get; set; }
        public string? SubmitButton { get; set; }
        public string InvoiceLinks { get; set; } = "a[href*='factura'], a[href*='invoice'], a[href$='.pdf']";
        public string? NextPage { get; set; }
        public bool RequiresBrowserDownload { get; set; } = false;
    }

    private class PortalInvoiceLink
    {
        public string Url { get; set; } = string.Empty;
        public string? FileName { get; set; }
        public bool RequiresBrowser { get; set; }
    }
}

