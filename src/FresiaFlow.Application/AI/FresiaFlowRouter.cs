using FresiaFlow.Application.Ports.Outbound;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FresiaFlow.Application.AI;

/// <summary>
/// Router de agentes FresiaFlow.
/// Selecciona el agente adecuado según el tipo de tarea y procesa el mensaje con histórico conversacional.
/// </summary>
public interface IFresiaFlowRouter
{
    /// <summary>
    /// Procesa un mensaje del usuario usando el router para seleccionar el agente adecuado.
    /// </summary>
    Task<RouterResponse> ProcessMessageAsync(
        string userMessage,
        List<ChatMessageHistory> history,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Respuesta del router con el agente seleccionado y su respuesta.
/// </summary>
public class RouterResponse
{
    public string Content { get; set; } = string.Empty;
    public string SelectedAgent { get; set; } = string.Empty;
}

/// <summary>
/// DTO para mensajes del historial.
/// </summary>
public class ChatMessageHistory
{
    public string Role { get; set; } = string.Empty; // "user" o "assistant"
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Implementación del router FresiaFlow.
/// </summary>
public class FresiaFlowRouter : IFresiaFlowRouter
{
    private readonly IOpenAIClient _openAIClient;
    private readonly ILogger<FresiaFlowRouter> _logger;

    // Agentes disponibles según .cursor/rules.md y .cursor/prompts.md
    private static readonly Dictionary<string, AgentDefinition> Agents = new()
    {
        { "ARQ", new AgentDefinition("ARQ", "Arquitecto Hexagonal (.NET)", 
            "Arquitectura, capas, dependencias, estructura, hexagonal, DDD, .NET") },
        { "DOM", new AgentDefinition("DOM", "Experto Dominio Facturación PyME", 
            "Entidades, facturas, impuestos, reglas de negocio, contabilidad, fiscal") },
        { "INT", new AgentDefinition("INT", "Ingeniero de Integraciones", 
            "APIs externas, bancos, OCR, resiliencia, integraciones, seguridad") },
        { "IA", new AgentDefinition("IA", "Especialista IA Aplicada (LLM + RAG)", 
            "OpenAI, prompts, PDFs, RAG, extracción, LLM, embeddings") },
        { "REV", new AgentDefinition("REV", "Code Reviewer Implacable", 
            "Revisión, mejora, código, calidad, refactoring") },
        { "COG", new AgentDefinition("COG", "Optimizador Cognitivo", 
            "Bloqueo mental, pasos, claridad, planificación, ayuda") },
        { "PO", new AgentDefinition("PO", "Product Owner Técnico", 
            "Prioridades, MVP, valor de negocio, requisitos, producto") },
        { "TEST", new AgentDefinition("TEST", "Experto Tester Automático", 
            "Tests, pruebas, cobertura, validación, unitarios, integración") },
        { "DOC", new AgentDefinition("DOC", "Documentador de Código", 
            "Documentación técnica, XML comments, APIs, guías") },
        { "AYU", new AgentDefinition("AYU", "Generador de Ayudas de Usuario", 
            "Ayudas usuario, guías, FAQs, contenido web, manuales") }
    };

    public FresiaFlowRouter(
        IOpenAIClient openAIClient,
        ILogger<FresiaFlowRouter> logger)
    {
        _openAIClient = openAIClient;
        _logger = logger;
    }

    public async Task<RouterResponse> ProcessMessageAsync(
        string userMessage,
        List<ChatMessageHistory> history,
        CancellationToken cancellationToken = default)
    {
        // 1. Seleccionar agente basado en el mensaje y el histórico
        var selectedAgent = SelectAgent(userMessage, history);

        // 2. Construir el system prompt del agente seleccionado
        var systemPrompt = BuildAgentSystemPrompt(selectedAgent);

        // 3. Construir mensajes con histórico
        var messages = BuildMessagesWithHistory(systemPrompt, userMessage, history);

        // 4. Llamar a OpenAI con el histórico
        var response = await CallOpenAIWithHistoryAsync(messages, cancellationToken);

        // 5. Extraer agente de la respuesta si está en formato [AGENTE]: ...
        var (content, agent) = ParseResponse(response, selectedAgent);

        return new RouterResponse
        {
            Content = content,
            SelectedAgent = agent
        };
    }

    private string SelectAgent(string userMessage, List<ChatMessageHistory> history)
    {
        // Combinar mensaje actual con histórico reciente para contexto
        var context = userMessage.ToLowerInvariant();
        if (history.Count > 0)
        {
            var recentHistory = history.TakeLast(4).Select(h => h.Content.ToLowerInvariant());
            context = string.Join(" ", recentHistory) + " " + context;
        }

        // Puntuación por agente
        var scores = new Dictionary<string, int>();

        foreach (var agent in Agents.Values)
        {
            var score = 0;
            var keywords = agent.Keywords.ToLowerInvariant().Split(',', StringSplitOptions.RemoveEmptyEntries);

            foreach (var keyword in keywords)
            {
                var trimmedKeyword = keyword.Trim();
                if (context.Contains(trimmedKeyword))
                {
                    score += 2; // Peso mayor para coincidencias exactas
                }
            }

            // Búsqueda de patrones específicos
            if (agent.Code == "ARQ" && (context.Contains("arquitectura") || context.Contains("hexagonal") || 
                context.Contains("capas") || context.Contains("ddd")))
                score += 5;

            if (agent.Code == "DOM" && (context.Contains("factura") || context.Contains("iva") || 
                context.Contains("impuesto") || context.Contains("contabilidad")))
                score += 5;

            if (agent.Code == "INT" && (context.Contains("api") || context.Contains("banco") || 
                context.Contains("integración") || context.Contains("ocr")))
                score += 5;

            if (agent.Code == "IA" && (context.Contains("openai") || context.Contains("rag") || 
                context.Contains("pdf") || context.Contains("prompt")))
                score += 5;

            if (agent.Code == "REV" && (context.Contains("revisar") || context.Contains("mejorar") || 
                context.Contains("código") || context.Contains("refactor")))
                score += 5;

            if (agent.Code == "COG" && (context.Contains("ayuda") || context.Contains("bloqueo") || 
                context.Contains("no sé") || context.Contains("cómo")))
                score += 5;

            if (agent.Code == "PO" && (context.Contains("prioridad") || context.Contains("mvp") || 
                context.Contains("valor") || context.Contains("requisito")))
                score += 5;

            if (agent.Code == "TEST" && (context.Contains("test") || context.Contains("prueba") || 
                context.Contains("cobertura") || context.Contains("validación")))
                score += 5;

            if (agent.Code == "DOC" && (context.Contains("documentación") || context.Contains("documentar") || 
                context.Contains("comentario") || context.Contains("api")))
                score += 5;

            if (agent.Code == "AYU" && (context.Contains("ayuda usuario") || context.Contains("guía") || 
                context.Contains("faq") || context.Contains("manual")))
                score += 5;

            scores[agent.Code] = score;
        }

        // Seleccionar agente con mayor puntuación, o COG por defecto
        var selected = scores.OrderByDescending(s => s.Value).FirstOrDefault();
        return selected.Value > 0 ? selected.Key : "COG";
    }

    private string BuildAgentSystemPrompt(string agentCode)
    {
        var agent = Agents[agentCode];
        var routerPrompt = LoadRouterPrompt();
        var agentPrompt = LoadAgentPrompt(agentCode);

        return $@"{routerPrompt}

{agentPrompt}

IMPORTANTE: Responde EXCLUSIVAMENTE en el formato:
[AGENTE SELECCIONADO]: {agentCode}

<Tu respuesta completa desde el rol de {agent.Name}>";
    }

    private string LoadRouterPrompt()
    {
        return @"Actúas como un Router de Agentes para desarrollo de software profesional.

Tu única función es:
1. Analizar la petición del usuario
2. Identificar el tipo de tarea
3. Seleccionar el agente más adecuado
4. Responder EXCLUSIVAMENTE desde ese rol

Reglas estrictas:
- No mezclar agentes
- No explicar el enrutamiento
- No ser literario
- Ser claro, directo y accionable";
    }

    private string LoadAgentPrompt(string agentCode)
    {
        // Cargar prompts específicos de cada agente desde docs/agents/
        // Por ahora, prompts básicos
        return agentCode switch
        {
            "ARQ" => "Eres un arquitecto de software senior experto en arquitectura hexagonal, DDD ligero y .NET. Detecta violaciones de arquitectura, define puertos y adaptadores correctos.",
            "DOM" => "Eres un experto en facturación y contabilidad de micro-pymes en España. Valida entidades, detecta errores semánticos, define reglas de negocio reales.",
            "INT" => "Eres un ingeniero senior de integraciones externas. Diseña adaptadores robustos, maneja errores y reintentos, asegura idempotencia.",
            "IA" => "Eres un ingeniero de IA aplicada a procesos empresariales, especializado en LLMs y RAG. Diseña prompts efectivos, implementa RAG cuando sea necesario.",
            "REV" => "Eres un code reviewer implacable. Revisa código buscando bugs, mejoras, violaciones de principios SOLID, problemas de rendimiento.",
            "COG" => "Eres un optimizador cognitivo. Ayudas a desbloquear problemas, descomponer tareas complejas, clarificar objetivos.",
            "PO" => "Eres un Product Owner técnico. Priorizas trabajo, defines MVP, evalúas valor de negocio, validas requisitos.",
            "TEST" => "Eres un experto en testing automático. Diseñas tests unitarios, de integración, aseguras cobertura adecuada.",
            "DOC" => "Eres un documentador de código. Creas documentación técnica clara, XML comments, documentas APIs.",
            "AYU" => "Eres un generador de ayudas de usuario. Creas guías claras, FAQs, contenido web accesible.",
            _ => "Eres un asistente experto que ayuda con desarrollo de software."
        };
    }

    private List<object> BuildMessagesWithHistory(string systemPrompt, string userMessage, List<ChatMessageHistory> history)
    {
        var messages = new List<object>
        {
            new { role = "system", content = systemPrompt }
        };

        // Agregar histórico (últimos 10 mensajes para no exceder tokens)
        foreach (var msg in history.TakeLast(10))
        {
            messages.Add(new
            {
                role = msg.Role,
                content = msg.Content
            });
        }

        // Agregar mensaje actual
        messages.Add(new
        {
            role = "user",
            content = userMessage
        });

        return messages;
    }

    private async Task<string> CallOpenAIWithHistoryAsync(List<object> messages, CancellationToken cancellationToken)
    {
        // Usar el método GetChatCompletionAsync pero necesitamos uno que soporte histórico
        // Por ahora, extraer system prompt y último mensaje para compatibilidad
        var systemPrompt = "";
        var userMessage = "";
        var historyMessages = new List<object>();

        foreach (var msg in messages)
        {
            var msgDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                JsonSerializer.Serialize(msg));
            
            if (msgDict != null && msgDict.ContainsKey("role"))
            {
                var role = msgDict["role"].ToString();
                var content = msgDict.ContainsKey("content") ? msgDict["content"].ToString() : "";

                if (role == "system")
                {
                    systemPrompt = content ?? "";
                }
                else if (role == "user")
                {
                    if (string.IsNullOrEmpty(userMessage))
                    {
                        userMessage = content ?? "";
                    }
                    else
                    {
                        historyMessages.Add(msg);
                    }
                }
                else if (role == "assistant")
                {
                    historyMessages.Add(msg);
                }
            }
        }

        // Construir contexto del histórico
        var historyContext = "";
        if (historyMessages.Count > 0)
        {
            var historyText = string.Join("\n", historyMessages.Select(m => 
                JsonSerializer.Serialize(m)));
            historyContext = $"\n\nHistórico de la conversación:\n{historyText}\n\n";
        }

        // Llamar a OpenAI
        var fullUserMessage = historyContext + userMessage;
        return await _openAIClient.GetChatCompletionAsync(systemPrompt, fullUserMessage, cancellationToken);
    }

    private (string content, string agent) ParseResponse(string response, string defaultAgent)
    {
        // Buscar patrón [AGENTE]: en la respuesta
        var match = Regex.Match(response, @"\[([A-Z]+)\]:\s*(.+)", RegexOptions.Singleline);
        
        if (match.Success)
        {
            var agent = match.Groups[1].Value;
            var content = match.Groups[2].Value.Trim();
            return (content, agent);
        }

        // Si no hay formato, usar agente por defecto
        return (response, defaultAgent);
    }

    private record AgentDefinition(string Code, string Name, string Keywords);
}

