using FresiaFlow.Application.Ports.Inbound;
using FresiaFlow.Application.Ports.Outbound;
using FresiaFlow.Application.UseCases;
using FresiaFlow.Application.InvoicesReceived;
using FresiaFlow.Application.AI;
using FresiaFlow.Adapters.Outbound.Persistence;
using FresiaFlow.Adapters.Outbound.OpenAI;
using FresiaFlow.Adapters.Outbound.Banking;
using FresiaFlow.Adapters.Outbound.Storage;
using FresiaFlow.Adapters.Outbound.Rag;
using FresiaFlow.Adapters.Outbound.Pdf;
using FresiaFlow.Adapters.Outbound.Excel;
using FresiaFlow.Adapters.Outbound.OneDrive;
using FresiaFlow.Adapters.Outbound.InvoiceSources;
using FresiaFlow.Adapters.Outbound.WhatsApp;
using FresiaFlow.Adapters.Inbound.Api.Notifiers;
using FresiaFlow.Infrastructure.HostedServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FresiaFlow.Infrastructure;

/// <summary>
/// Configuración de inyección de dependencias.
/// Conecta los puertos con sus adaptadores.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddFresiaFlowInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<FresiaFlowDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Repositories (Outbound Ports -> Adapters)
        services.AddScoped<IInvoiceRepository, EfInvoiceRepository>();
        services.AddScoped<IIssuedInvoiceRepository, EfIssuedInvoiceRepository>();
        services.AddScoped<IBankTransactionRepository, EfBankTransactionRepository>();
        services.AddScoped<IBankAccountRepository, EfBankAccountRepository>();
        services.AddScoped<ITaskRepository, EfTaskRepository>();
        services.AddScoped<IAccountingEntryRepository, EfAccountingEntryRepository>();
        services.AddScoped<IAccountingAccountRepository, EfAccountingAccountRepository>();

        // Services
        services.AddSingleton<FresiaFlow.Application.Services.AccountingGenerationCancellationService>();

        // External Services - OpenAI
        services.AddHttpClient();
        
        // Configurar HttpClient con nombre "OpenAI" para InvoiceExtractionService
        services.AddHttpClient("OpenAI", client =>
        {
            client.BaseAddress = new Uri("https://api.openai.com");
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        // Configurar HttpClient con nombre "ChatAI" para acceso a internet del chat
        services.AddHttpClient("ChatAI", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            // Configuración de proxy se puede agregar aquí si es necesario
            var proxyUrl = configuration["ChatAI:InternetAccess:ProxyUrl"];
            if (!string.IsNullOrWhiteSpace(proxyUrl))
            {
                // Configurar proxy si está especificado
                // Nota: Requiere configuración adicional del HttpClientHandler
            }
        });
        
        // OpenAI Client para extracción de facturas y otros servicios
        services.AddScoped<IOpenAIClient>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri("https://api.openai.com");
            var apiKey = configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI:ApiKey no configurado");
            var model = configuration["OpenAI:Model"] ?? "gpt-4";
            return new OpenAIAdapter(httpClient, apiKey, model);
        });

        // OpenAI Client específico para Chat AI (puede usar API key diferente)
        services.AddScoped<IChatAIClient>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri("https://api.openai.com");
            // Usar ChatAI:OpenAI:ApiKey si existe, sino usar OpenAI:ApiKey como fallback
            var apiKey = configuration["ChatAI:OpenAI:ApiKey"] 
                ?? configuration["OpenAI:ApiKey"] 
                ?? throw new InvalidOperationException("OpenAI:ApiKey no configurado");
            var model = configuration["ChatAI:OpenAI:Model"] 
                ?? configuration["OpenAI:Model"] 
                ?? "gpt-4";
            var adapter = new OpenAIAdapter(httpClient, apiKey, model);
            // OpenAIAdapter implementa IOpenAIClient, que es lo que IChatAIClient extiende
            return adapter;
        });

        // External Services - Banking
        services.AddScoped<IBankProvider>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient();
            var providerName = configuration["Banking:Provider"] ?? "TrueLayer";
            var apiKey = configuration["Banking:ApiKey"] ?? throw new InvalidOperationException("Banking:ApiKey no configurado");
            var baseUrl = configuration["Banking:BaseUrl"] ?? "https://api.truelayer.com";
            httpClient.BaseAddress = new Uri(baseUrl);
            return new BankProviderAdapter(httpClient, providerName, apiKey);
        });

        // File Storage
        services.AddScoped<IFileStorage>(sp =>
        {
            var basePath = configuration["Storage:BasePath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads");
            return new FileStorageAdapter(basePath);
        });

        // Vector Store (RAG)
        services.AddScoped<IVectorStore>(sp =>
        {
            var openAIClient = sp.GetRequiredService<IOpenAIClient>();
            return new VectorStoreAdapter(openAIClient);
        });

        // Use Cases (Inbound Ports -> Implementations)
        services.AddScoped<IUploadInvoiceUseCase, UploadInvoiceUseCase>();
        services.AddScoped<UploadInvoiceUseCase>(); // También como clase concreta para OneDriveSyncService
        services.AddScoped<IImportIssuedInvoicesFromExcelUseCase, ImportIssuedInvoicesFromExcelUseCase>();
        services.AddScoped<IExportIssuedInvoicesUseCase, ExportIssuedInvoicesUseCase>();
        services.AddScoped<IGetAllInvoicesUseCase, GetAllInvoicesUseCase>();
        services.AddScoped<IGetFilteredInvoicesUseCase, GetFilteredInvoicesUseCase>();
        services.AddScoped<IDeleteInvoiceUseCase, DeleteInvoiceUseCase>();
        services.AddScoped<IUpdateInvoiceSupplierUseCase, UpdateInvoiceSupplierUseCase>();
        services.AddScoped<IUpdateInvoiceUseCase, UpdateInvoiceUseCase>();
        services.AddScoped<ISyncBankTransactionsUseCase, SyncBankTransactionsUseCase>();
        services.AddScoped<IProposeDailyPlanUseCase, ProposeDailyPlanUseCase>();
        services.AddScoped<IMarkInvoiceAsReviewedUseCase, MarkInvoiceAsReviewedUseCase>();
        services.AddScoped<ICreateTaskUseCase, CreateTaskUseCase>();
        services.AddScoped<ITaskManagementUseCase, TaskManagementUseCase>();
        services.AddScoped<IDashboardUseCase, DashboardUseCase>();
        // Generación de asientos contables - inyectar notificador de progreso y logger
        services.AddScoped<IGenerateAccountingEntriesUseCase>(sp =>
        {
            var invoiceRepo = sp.GetRequiredService<IInvoiceReceivedRepository>();
            var entryRepo = sp.GetRequiredService<IAccountingEntryRepository>();
            var accountRepo = sp.GetRequiredService<IAccountingAccountRepository>();
            var progressNotifier = sp.GetService<IAccountingProgressNotifier>();
            var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<GenerateAccountingEntriesUseCase>>();
            return new GenerateAccountingEntriesUseCase(invoiceRepo, entryRepo, accountRepo, progressNotifier, logger);
        });
        services.AddScoped<IGetAccountingEntriesUseCase, GetAccountingEntriesUseCase>();
        services.AddScoped<IUpdateAccountingEntryUseCase, UpdateAccountingEntryUseCase>();
        services.AddScoped<IPostAccountingEntryUseCase, PostAccountingEntryUseCase>();

        // Módulo de Facturas Recibidas
        services.AddScoped<IProcessIncomingInvoiceCommandHandler, ProcessIncomingInvoiceCommandHandler>();
        services.AddScoped<IInvoiceReceivedRepository, EfInvoiceReceivedRepository>();
        services.AddScoped<IInvoiceProcessingSnapshotRepository, EfInvoiceProcessingSnapshotRepository>();
        services.AddScoped<IPdfTextExtractorService, PdfTextExtractorService>();
        services.AddScoped<IDocumentClassificationService, DocumentClassificationService>();
        services.AddScoped<IInvoiceExtractionService, InvoiceExtractionService>();

        // Procesamiento de Excel
        services.AddScoped<IExcelProcessor, ExcelProcessorService>();
        services.AddScoped<IExcelExporter, ExcelExporterService>();

        // Notificaciones de progreso (SignalR)
        services.AddScoped<ISyncProgressNotifier, SignalRSyncProgressNotifier>();
        services.AddScoped<IAccountingProgressNotifier, SignalRAccountingProgressNotifier>();

        // AI Orchestrator
        services.AddScoped<ToolRegistry>(sp =>
        {
            var getFilteredInvoicesUseCase = sp.GetRequiredService<IGetFilteredInvoicesUseCase>();
            var getAllInvoicesUseCase = sp.GetRequiredService<IGetAllInvoicesUseCase>();
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var configuration = sp.GetRequiredService<IConfiguration>();
            var webSearchLogger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Application.AI.Tools.WebSearchTool>>();
            return new ToolRegistry(
                getFilteredInvoicesUseCase, 
                getAllInvoicesUseCase,
                httpClientFactory,
                configuration,
                webSearchLogger);
        });
        services.AddScoped<IFresiaFlowOrchestrator, FresiaFlowOrchestrator>();
        
        // FresiaFlow Router - usa IChatAIClient (API key específica para chat)
        services.AddScoped<IFresiaFlowRouter>(sp =>
        {
            var chatAIClient = sp.GetRequiredService<IChatAIClient>();
            var toolRegistry = sp.GetRequiredService<ToolRegistry>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<FresiaFlowRouter>>();
            return new FresiaFlowRouter(chatAIClient, toolRegistry, logger);
        });

        // Configuración de opciones
        services.Configure<IncomingInvoiceOptions>(
            configuration.GetSection(IncomingInvoiceOptions.SectionName));
        services.Configure<OpenAiOptions>(
            configuration.GetSection(OpenAiOptions.SectionName));
        services.Configure<InvoiceExtractionPromptOptions>(
            configuration.GetSection(InvoiceExtractionPromptOptions.SectionName));
        services.Configure<InvoiceProcessingOptions>(
            configuration.GetSection(InvoiceProcessingOptions.SectionName));
        services.Configure<DocumentClassificationOptions>(
            configuration.GetSection(DocumentClassificationOptions.SectionName));

        // Hosted Services
        services.AddHostedService<FileSystemInvoiceWatcherService>();

        // OneDrive Sync
        services.AddHttpClient("OneDrive", client =>
        {
            client.Timeout = TimeSpan.FromMinutes(10); // Aumentado a 10 minutos para archivos grandes y operaciones de refresco
        });
        services.AddScoped<IOneDriveSyncService, OneDriveSyncService>();
        services.AddHostedService<OneDriveSyncBackgroundService>();

        // WhatsApp Notifications
        services.AddHttpClient("WhatsApp", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddScoped<IWhatsAppNotificationService, MetaWhatsAppService>();

        // Invoice Sources Sync Services
        services.AddScoped<FresiaFlow.Adapters.Outbound.InvoiceSources.EmailSyncService>();
        services.AddScoped<FresiaFlow.Adapters.Outbound.InvoiceSources.WebScrapingSyncService>();
        services.AddScoped<FresiaFlow.Adapters.Outbound.InvoiceSources.PortalSyncService>();
        services.AddScoped<FresiaFlow.Adapters.Outbound.InvoiceSources.OneDriveInvoiceSourceSyncService>();
        services.AddScoped<FresiaFlow.Adapters.Outbound.InvoiceSources.InvoiceSourceSyncServiceFactory>();
        services.AddScoped<FresiaFlow.Adapters.Outbound.InvoiceSources.SyncInvoicesFromSourcesUseCase>();

        return services;
    }
}

