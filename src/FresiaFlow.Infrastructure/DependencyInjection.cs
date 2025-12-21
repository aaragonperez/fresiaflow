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
        services.AddScoped<IBankTransactionRepository, EfBankTransactionRepository>();
        services.AddScoped<IBankAccountRepository, EfBankAccountRepository>();
        services.AddScoped<ITaskRepository, EfTaskRepository>();

        // External Services - OpenAI
        services.AddHttpClient();
        
        // Configurar HttpClient con nombre "OpenAI" para InvoiceExtractionService
        services.AddHttpClient("OpenAI", client =>
        {
            client.BaseAddress = new Uri("https://api.openai.com");
            client.Timeout = TimeSpan.FromSeconds(60);
        });
        
        services.AddScoped<IOpenAIClient>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri("https://api.openai.com");
            var apiKey = configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI:ApiKey no configurado");
            var model = configuration["OpenAI:Model"] ?? "gpt-4";
            return new OpenAIAdapter(httpClient, apiKey, model);
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
        services.AddScoped<IGetAllInvoicesUseCase, GetAllInvoicesUseCase>();
        services.AddScoped<ISyncBankTransactionsUseCase, SyncBankTransactionsUseCase>();
        services.AddScoped<IProposeDailyPlanUseCase, ProposeDailyPlanUseCase>();
        services.AddScoped<IMarkInvoiceAsReviewedUseCase, MarkInvoiceAsReviewedUseCase>();

        // Módulo de Facturas Recibidas
        services.AddScoped<IProcessIncomingInvoiceCommandHandler, ProcessIncomingInvoiceCommandHandler>();
        services.AddScoped<IInvoiceReceivedRepository, EfInvoiceReceivedRepository>();
        services.AddScoped<IPdfTextExtractorService, PdfTextExtractorService>();
        services.AddScoped<IInvoiceExtractionService, InvoiceExtractionService>();

        // AI Orchestrator
        services.AddScoped<ToolRegistry>();
        services.AddScoped<IFresiaFlowOrchestrator, FresiaFlowOrchestrator>();

        // Configuración de opciones
        services.Configure<IncomingInvoiceOptions>(
            configuration.GetSection(IncomingInvoiceOptions.SectionName));
        services.Configure<OpenAiOptions>(
            configuration.GetSection(OpenAiOptions.SectionName));
        services.Configure<InvoiceExtractionPromptOptions>(
            configuration.GetSection(InvoiceExtractionPromptOptions.SectionName));

        // Hosted Services
        services.AddHostedService<FileSystemInvoiceWatcherService>();

        return services;
    }
}

