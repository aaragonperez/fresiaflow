using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FresiaFlow.Adapters.Outbound.Persistence;
using FresiaFlow.Application.Ports.Inbound;
using FresiaFlow.Application.Ports.Outbound;
using FresiaFlow.Domain.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MimeKit;
using MailKit.Net.Imap;
using MailKit.Net.Pop3;
using MailKit.Search;
using MailKit;

namespace FresiaFlow.Adapters.Outbound.InvoiceSources;

/// <summary>
/// Servicio de sincronización de facturas desde email (IMAP/POP3).
/// </summary>
public class EmailSyncService : IInvoiceSourceSyncService
{
    private readonly FresiaFlowDbContext _dbContext;
    private readonly IUploadInvoiceUseCase _uploadInvoiceUseCase;
    private readonly ILogger<EmailSyncService> _logger;
    private readonly ISyncProgressNotifier _progressNotifier;

    private static readonly string[] SupportedExtensions = { ".pdf", ".jpg", ".jpeg", ".png", ".gif", ".webp" };

    public EmailSyncService(
        FresiaFlowDbContext dbContext,
        IUploadInvoiceUseCase uploadInvoiceUseCase,
        ILogger<EmailSyncService> logger,
        ISyncProgressNotifier progressNotifier)
    {
        _dbContext = dbContext;
        _uploadInvoiceUseCase = uploadInvoiceUseCase;
        _logger = logger;
        _progressNotifier = progressNotifier;
    }

    public async Task<InvoiceSourceConfig?> GetConfigAsync(Guid sourceId)
    {
        return await _dbContext.InvoiceSourceConfigs
            .FirstOrDefaultAsync(c => c.Id == sourceId && c.SourceType == InvoiceSourceType.Email);
    }

    public async Task<InvoiceSourceConfig> SaveConfigAsync(InvoiceSourceConfig config)
    {
        if (config.SourceType != InvoiceSourceType.Email)
        {
            throw new ArgumentException("El tipo de fuente debe ser Email", nameof(config));
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
                preview.ErrorMessage = "Configuración de email no encontrada o deshabilitada";
                return preview;
            }

            var emailConfig = DeserializeEmailConfig(config.ConfigJson);
            if (emailConfig == null)
            {
                preview.ErrorMessage = "Configuración de email inválida";
                return preview;
            }

            // Conectar y contar emails con facturas
            var emails = await ListInvoiceEmailsAsync(emailConfig, cancellationToken);
            preview.TotalFiles = emails.Count;

            // Filtrar por extensiones soportadas
            var supportedEmails = emails.Where(e => 
                SupportedExtensions.Any(ext => e.AttachmentName?.EndsWith(ext, StringComparison.OrdinalIgnoreCase) == true)
            ).ToList();

            preview.SupportedFiles = supportedEmails.Count;

            // Verificar cuántos ya están sincronizados
            var attachmentIds = supportedEmails.Select(e => e.AttachmentId).ToList();
            var syncedCount = await _dbContext.SyncedFiles
                .Where(s => s.Source == $"Email-{sourceId}" && attachmentIds.Contains(s.ExternalId))
                .CountAsync(cancellationToken);

            preview.AlreadySynced = syncedCount;
            preview.PendingToProcess = preview.SupportedFiles - preview.AlreadySynced;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo preview de sincronización de email");
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
            result.ErrorMessage = "Configuración de email no encontrada o deshabilitada";
            return result;
        }

        var emailConfig = DeserializeEmailConfig(config.ConfigJson);
        if (emailConfig == null)
        {
            result.Success = false;
            result.ErrorMessage = "Configuración de email inválida";
            return result;
        }

        try
        {
            _logger.LogInformation("Iniciando sincronización de email: {SourceName}", config.Name);

            // Listar emails con facturas
            var emails = await ListInvoiceEmailsAsync(emailConfig, cancellationToken);
            var supportedEmails = emails.Where(e => 
                SupportedExtensions.Any(ext => e.AttachmentName?.EndsWith(ext, StringComparison.OrdinalIgnoreCase) == true)
            ).ToList();

            var totalFiles = supportedEmails.Count;
            var processedFiles = 0;

            await _progressNotifier.NotifyAsync(new SyncProgressUpdate
            {
                CurrentFile = "Iniciando sincronización de email...",
                ProcessedCount = 0,
                TotalCount = totalFiles,
                Percentage = 0,
                Status = "syncing",
                Message = $"Se encontraron {totalFiles} facturas en email"
            }, cancellationToken);

            foreach (var email in supportedEmails)
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
                        .FirstOrDefaultAsync(s => s.Source == $"Email-{sourceId}" && s.ExternalId == email.AttachmentId, cancellationToken);

                    if (existingSync != null)
                    {
                        // Si ya existe y está completado
                        if (existingSync.Status == SyncStatus.Completed)
                        {
                            // Si se fuerza el reproceso, reprocesar
                            if (forceReprocess)
                            {
                                _logger.LogInformation("Forzando reproceso de: {AttachmentName}", email.AttachmentName);
                                existingSync.MarkAsProcessing();
                            }
                            // Si NO se fuerza, saltar (ya procesado)
                            else
                            {
                                _logger.LogDebug("Saltando archivo ya procesado: {AttachmentName}", email.AttachmentName);
                                result.SkippedCount++;
                                processedFiles++;
                                continue;
                            }
                        }
                        // Si existe pero falló o está pendiente, reprocesar
                        else
                        {
                            _logger.LogInformation("Reprocesando archivo con estado {Status}: {AttachmentName}", existingSync.Status, email.AttachmentName);
                            existingSync.MarkAsProcessing();
                        }
                    }

                    // Descargar adjunto
                    var attachmentData = await DownloadAttachmentAsync(emailConfig, email.MessageId, email.AttachmentId, cancellationToken);
                    if (attachmentData == null)
                    {
                        if (existingSync != null)
                        {
                            existingSync.MarkAsFailed("No se pudo descargar el adjunto");
                        }
                        result.FailedCount++;
                        result.DetailedErrors.Add($"{email.AttachmentName}: No se pudo descargar el adjunto");
                        continue;
                    }

                    // Procesar factura
                    using var fileStream = new MemoryStream(attachmentData);
                    var uploadCommand = new UploadInvoiceCommand(
                        fileStream,
                        email.AttachmentName ?? "factura.pdf",
                        GetContentType(email.AttachmentName ?? "factura.pdf"));

                    var invoiceResult = await _uploadInvoiceUseCase.ExecuteAsync(uploadCommand);

                    // Registrar como sincronizado
                    if (existingSync == null)
                    {
                        existingSync = SyncedFile.Create(
                            $"Email-{sourceId}",
                            email.AttachmentId,
                            email.AttachmentName ?? "factura.pdf",
                            $"Email: {email.Subject}",
                            attachmentData.Length,
                            ComputeHash(attachmentData),
                            email.Date
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
                        .FirstOrDefaultAsync(s => s.Source == $"Email-{sourceId}" && s.ExternalId == email.AttachmentId, cancellationToken);
                    
                    var errorMessage = ex.Message;
                    if (ex.InnerException != null)
                    {
                        errorMessage += $" -> {ex.InnerException.Message}";
                    }
                    
                    if (syncFile != null)
                    {
                        syncFile.MarkAsFailed(errorMessage);
                    }
                    
                    _logger.LogWarning(ex, "Error procesando email {MessageId}: {Error}", email.MessageId, errorMessage);
                    result.FailedCount++;
                    result.DetailedErrors.Add($"{email.AttachmentName}: {errorMessage}");
                }

                // Actualizar progreso
                var percentage = totalFiles > 0 ? (int)((double)processedFiles / totalFiles * 100) : 0;
                await _progressNotifier.NotifyAsync(new SyncProgressUpdate
                {
                    CurrentFile = email.AttachmentName ?? "factura",
                    ProcessedCount = processedFiles,
                    TotalCount = totalFiles,
                    Percentage = percentage,
                    Status = "syncing",
                    Message = $"Procesado: {email.AttachmentName}"
                }, cancellationToken);
            }

            config.RecordSuccessfulSync(result.ProcessedCount);
            result.Success = result.FailedCount == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante sincronización de email");
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
            var config = DeserializeEmailConfig(configJson);
            if (config == null)
            {
                return new SourceValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Configuración JSON inválida"
                };
            }

            // Intentar conectar al servidor de email
            if (config.UseImap)
            {
                using var client = new ImapClient();
                await client.ConnectAsync(config.ImapServer, config.ImapPort, config.UseSsl, cancellationToken);
                await client.AuthenticateAsync(config.Username, config.Password, cancellationToken);
                await client.DisconnectAsync(true, cancellationToken);
            }
            else
            {
                using var client = new Pop3Client();
                await client.ConnectAsync(config.Pop3Server, config.Pop3Port, config.UseSsl, cancellationToken);
                await client.AuthenticateAsync(config.Username, config.Password, cancellationToken);
                await client.DisconnectAsync(true, cancellationToken);
            }

            return new SourceValidationResult
            {
                IsValid = true,
                Info = new Dictionary<string, object>
                {
                    { "server", config.UseImap ? config.ImapServer : config.Pop3Server },
                    { "port", config.UseImap ? config.ImapPort : config.Pop3Port }
                }
            };
        }
        catch (Exception ex)
        {
            return new SourceValidationResult
            {
                IsValid = false,
                ErrorMessage = $"Error de conexión: {ex.Message}"
            };
        }
    }

    private async Task<List<EmailInvoiceInfo>> ListInvoiceEmailsAsync(EmailConfig config, CancellationToken cancellationToken)
    {
        var emails = new List<EmailInvoiceInfo>();

        if (config.UseImap)
        {
            using var client = new ImapClient();
            await client.ConnectAsync(config.ImapServer, config.ImapPort, config.UseSsl, cancellationToken);
            await client.AuthenticateAsync(config.Username, config.Password, cancellationToken);

            var inbox = client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadOnly, cancellationToken);

            // Construir query de búsqueda
            // MailKit requiere que todas las queries sean del mismo tipo base
            // Simplificamos: si hay filtros, usamos el primero; si hay múltiples, los combinamos manualmente después
            SearchQuery finalQuery = SearchQuery.All;
            
            if (config.Filter?.From?.Any() == true)
            {
                // Usar solo el primer "From" para simplificar
                // Para múltiples, se pueden procesar después filtrando manualmente
                finalQuery = SearchQuery.FromContains(config.Filter.From.First());
            }

            if (config.Filter?.SubjectContains?.Any() == true)
            {
                // Si ya hay un query de From, combinamos con AND
                if (finalQuery != SearchQuery.All)
                {
                    finalQuery = SearchQuery.And(finalQuery, SearchQuery.SubjectContains(config.Filter.SubjectContains.First()));
                }
                else
                {
                    finalQuery = SearchQuery.SubjectContains(config.Filter.SubjectContains.First());
                }
            }
            var uids = await inbox.SearchAsync(finalQuery, cancellationToken);
            var messages = await inbox.FetchAsync(uids, MessageSummaryItems.Envelope | MessageSummaryItems.BodyStructure, cancellationToken);

            foreach (var message in messages)
            {
                var fullMessage = await inbox.GetMessageAsync(message.UniqueId, cancellationToken);
                
                // Verificar si tiene adjuntos si el filtro lo requiere
                if (config.Filter?.HasAttachment == true && !fullMessage.Attachments.Any())
                    continue;

                var attachments = fullMessage.Attachments.OfType<MimeEntity>();

                foreach (var attachment in attachments)
                {
                    if (attachment is MimePart part && part.FileName != null)
                    {
                        var ext = Path.GetExtension(part.FileName).ToLowerInvariant();
                        if (SupportedExtensions.Contains(ext))
                        {
                            emails.Add(new EmailInvoiceInfo
                            {
                                MessageId = message.UniqueId.Id.ToString(),
                                AttachmentId = $"{message.UniqueId.Id}-{part.ContentId ?? part.FileName}",
                                AttachmentName = part.FileName,
                                Subject = message.Envelope.Subject ?? "",
                                From = message.Envelope.From.ToString(),
                                Date = message.Envelope.Date?.UtcDateTime ?? DateTime.UtcNow
                            });
                        }
                    }
                }
            }

            await client.DisconnectAsync(true, cancellationToken);
        }
        else
        {
            using var client = new Pop3Client();
            await client.ConnectAsync(config.Pop3Server, config.Pop3Port, config.UseSsl, cancellationToken);
            await client.AuthenticateAsync(config.Username, config.Password, cancellationToken);

            var count = await client.GetMessageCountAsync(cancellationToken);
            for (int i = 0; i < count; i++)
            {
                var message = await client.GetMessageAsync(i, cancellationToken);
                var attachments = message.Attachments.OfType<MimeEntity>();

                foreach (var attachment in attachments)
                {
                    if (attachment is MimePart part && part.FileName != null)
                    {
                        var ext = Path.GetExtension(part.FileName).ToLowerInvariant();
                        if (SupportedExtensions.Contains(ext))
                        {
                            emails.Add(new EmailInvoiceInfo
                            {
                                MessageId = i.ToString(),
                                AttachmentId = $"{i}-{part.ContentId ?? part.FileName}",
                                AttachmentName = part.FileName,
                                Subject = message.Subject ?? "",
                                From = message.From.ToString(),
                                Date = message.Date.DateTime
                            });
                        }
                    }
                }
            }

            await client.DisconnectAsync(true, cancellationToken);
        }

        return emails;
    }

    private async Task<byte[]?> DownloadAttachmentAsync(EmailConfig config, string messageId, string attachmentId, CancellationToken cancellationToken)
    {
        try
        {
            if (config.UseImap)
            {
                using var client = new ImapClient();
                await client.ConnectAsync(config.ImapServer, config.ImapPort, config.UseSsl, cancellationToken);
                await client.AuthenticateAsync(config.Username, config.Password, cancellationToken);

                var inbox = client.Inbox;
                await inbox.OpenAsync(FolderAccess.ReadOnly, cancellationToken);

                var uid = new UniqueId(uint.Parse(messageId));
                var message = await inbox.GetMessageAsync(uid, cancellationToken);

                var attachmentName = attachmentId.Split('-').LastOrDefault() ?? "attachment";
                var attachment = message.Attachments.OfType<MimePart>()
                    .FirstOrDefault(a => a.FileName == attachmentName || a.ContentId == attachmentName);

                if (attachment != null)
                {
                    using var stream = new MemoryStream();
                    await attachment.Content.DecodeToAsync(stream, cancellationToken);
                    await client.DisconnectAsync(true, cancellationToken);
                    return stream.ToArray();
                }

                await client.DisconnectAsync(true, cancellationToken);
            }
            else
            {
                using var client = new Pop3Client();
                await client.ConnectAsync(config.Pop3Server, config.Pop3Port, config.UseSsl, cancellationToken);
                await client.AuthenticateAsync(config.Username, config.Password, cancellationToken);

                var messageIndex = int.Parse(messageId);
                var message = await client.GetMessageAsync(messageIndex, cancellationToken);

                var attachmentName = attachmentId.Split('-').LastOrDefault() ?? "attachment";
                var attachment = message.Attachments.OfType<MimePart>()
                    .FirstOrDefault(a => a.FileName == attachmentName || a.ContentId == attachmentName);

                if (attachment != null)
                {
                    using var stream = new MemoryStream();
                    await attachment.Content.DecodeToAsync(stream, cancellationToken);
                    await client.DisconnectAsync(true, cancellationToken);
                    return stream.ToArray();
                }

                await client.DisconnectAsync(true, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error descargando adjunto {AttachmentId}", attachmentId);
        }

        return null;
    }

    private EmailConfig? DeserializeEmailConfig(string configJson)
    {
        try
        {
            return JsonSerializer.Deserialize<EmailConfig>(configJson, new JsonSerializerOptions
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

    private class EmailConfig
    {
        public bool UseImap { get; set; }
        public string ImapServer { get; set; } = string.Empty;
        public int ImapPort { get; set; } = 993;
        public string Pop3Server { get; set; } = string.Empty;
        public int Pop3Port { get; set; } = 110;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool UseSsl { get; set; } = true;
        public string Folder { get; set; } = "INBOX";
        public EmailFilter? Filter { get; set; }
    }

    private class EmailFilter
    {
        public List<string>? From { get; set; }
        public List<string>? SubjectContains { get; set; }
        public bool HasAttachment { get; set; } = true;
    }

    private class EmailInvoiceInfo
    {
        public string MessageId { get; set; } = string.Empty;
        public string AttachmentId { get; set; } = string.Empty;
        public string? AttachmentName { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string From { get; set; } = string.Empty;
        public DateTime Date { get; set; }
    }
}

