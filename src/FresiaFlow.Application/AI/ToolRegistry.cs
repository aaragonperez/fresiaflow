using FresiaFlow.Application.Ports.Outbound;

namespace FresiaFlow.Application.AI;

/// <summary>
/// Registro de herramientas disponibles para OpenAI tool calling.
/// Centraliza la definición y ejecución de herramientas.
/// </summary>
public class ToolRegistry
{
    private readonly Dictionary<string, Func<string, CancellationToken, Task<string>>> _tools;

    public ToolRegistry()
    {
        _tools = new Dictionary<string, Func<string, CancellationToken, Task<string>>>();
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
        // Las herramientas se ejecutarán a través de los casos de uso
        // Este es un stub que se completará con inyección de dependencias
    }
}

