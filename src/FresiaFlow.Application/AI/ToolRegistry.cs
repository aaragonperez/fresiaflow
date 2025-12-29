using FresiaFlow.Application.Ports.Inbound;
using FresiaFlow.Application.Ports.Outbound;
using FresiaFlow.Application.AI.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace FresiaFlow.Application.AI;

/// <summary>
/// Registro de herramientas disponibles para OpenAI tool calling.
/// Centraliza la definición y ejecución de herramientas.
/// </summary>
public class ToolRegistry
{
    private readonly Dictionary<string, Func<string, CancellationToken, Task<string>>> _tools;
    private readonly InvoiceFilterTool _invoiceFilterTool;
    private readonly InvoiceSearchTool _invoiceSearchTool;
    private readonly WebSearchTool? _webSearchTool;

    public ToolRegistry(
        IGetFilteredInvoicesUseCase getFilteredInvoicesUseCase,
        IGetAllInvoicesUseCase getAllInvoicesUseCase,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<WebSearchTool> webSearchLogger)
    {
        _tools = new Dictionary<string, Func<string, CancellationToken, Task<string>>>();
        _invoiceFilterTool = new InvoiceFilterTool(getFilteredInvoicesUseCase);
        _invoiceSearchTool = new InvoiceSearchTool(getAllInvoicesUseCase);
        
        // Inicializar WebSearchTool solo si el acceso a internet está habilitado
        var internetEnabled = configuration.GetValue<bool>("ChatAI:InternetAccess:Enabled", true);
        if (internetEnabled)
        {
            _webSearchTool = new WebSearchTool(httpClientFactory, configuration, webSearchLogger);
        }
        
        RegisterDefaultTools();
    }

    /// <summary>
    /// Obtiene las definiciones de herramientas para OpenAI.
    /// </summary>
    public List<ToolDefinition> GetAvailableTools()
    {
        return new List<ToolDefinition>
        {
            new ToolDefinition(
                "filter_invoices",
                "Filtra facturas recibidas por año, trimestre, proveedor o tipo de pago. Ejecuta la acción en la aplicación para aplicar los filtros en la tabla.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        year = new { type = "integer", description = "Año fiscal (ej: 2024)" },
                        quarter = new { type = "integer", @enum = new[] { 1, 2, 3, 4 }, description = "Trimestre (1-4)" },
                        supplierName = new { type = "string", description = "Nombre del proveedor" },
                        paymentType = new { type = "string", @enum = new[] { "Bank", "Cash" }, description = "Tipo de pago: Bank o Cash" }
                    }
                }
            ),
            new ToolDefinition(
                "search_invoices",
                "Busca facturas por texto libre. Busca en número de factura, nombre del proveedor, NIF/CIF o notas.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        query = new { type = "string", description = "Texto de búsqueda" }
                    },
                    required = new[] { "query" }
                }
            ),
            new ToolDefinition(
                "web_search",
                "Realiza una búsqueda en internet para obtener información actualizada. Útil para consultar información que no está en la base de datos local.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        query = new { type = "string", description = "Consulta de búsqueda en internet" }
                    },
                    required = new[] { "query" }
                }
            ),
            new ToolDefinition(
                "get_pending_invoices",
                "Obtiene la lista de facturas pendientes de pago",
                new
                {
                    type = "object",
                    properties = new { }
                }
            ),
            new ToolDefinition(
                "get_unreconciled_transactions",
                "Obtiene transacciones bancarias sin conciliar",
                new
                {
                    type = "object",
                    properties = new { }
                }
            ),
            new ToolDefinition(
                "create_task",
                "Crea una nueva tarea en el sistema",
                new
                {
                    type = "object",
                    properties = new
                    {
                        title = new { type = "string", description = "Título de la tarea" },
                        description = new { type = "string", description = "Descripción opcional" },
                        priority = new { type = "string", @enum = new[] { "Low", "Medium", "High", "Urgent" } }
                    },
                    required = new[] { "title" }
                }
            )
        };
    }

    /// <summary>
    /// Ejecuta una herramienta por nombre.
    /// </summary>
    public async Task<string> ExecuteToolAsync(string toolName, string argumentsJson, CancellationToken cancellationToken = default)
    {
        if (!_tools.ContainsKey(toolName))
        {
            return $"Error: Herramienta '{toolName}' no encontrada.";
        }

        try
        {
            return await _tools[toolName](argumentsJson, cancellationToken);
        }
        catch (Exception ex)
        {
            return $"Error ejecutando '{toolName}': {ex.Message}";
        }
    }

    /// <summary>
    /// Registra una nueva herramienta personalizada.
    /// </summary>
    public void RegisterTool(string name, Func<string, CancellationToken, Task<string>> executor)
    {
        _tools[name] = executor;
    }

    private void RegisterDefaultTools()
    {
        // Registrar herramientas de filtrado y búsqueda de facturas
        _tools["filter_invoices"] = _invoiceFilterTool.ExecuteAsync;
        _tools["search_invoices"] = _invoiceSearchTool.ExecuteAsync;
        
        // Registrar herramienta de búsqueda web si está disponible
        if (_webSearchTool != null)
        {
            _tools["web_search"] = _webSearchTool.ExecuteAsync;
        }
        
        // Otras herramientas se pueden registrar aquí cuando se implementen
        // _tools["get_pending_invoices"] = ...
        // _tools["get_unreconciled_transactions"] = ...
        // _tools["create_task"] = ...
    }
}

