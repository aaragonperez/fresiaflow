using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FresiaFlow.Application.AI.Tools;

/// <summary>
/// Herramienta para realizar búsquedas web.
/// Soporta múltiples proveedores: SerpAPI, DuckDuckGo, Bing Search API.
/// </summary>
public class WebSearchTool
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WebSearchTool> _logger;
    private readonly string _provider;
    private readonly string? _apiKey;

    public WebSearchTool(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<WebSearchTool> logger)
    {
        _httpClient = httpClientFactory.CreateClient("ChatAI");
        _configuration = configuration;
        _logger = logger;
        
        // Leer configuración del proveedor de búsqueda
        _provider = configuration["ChatAI:WebSearch:Provider"] ?? "DuckDuckGo";
        _apiKey = configuration["ChatAI:WebSearch:ApiKey"];
        
        var enabled = configuration.GetValue<bool>("ChatAI:InternetAccess:Enabled", true);
        if (!enabled)
        {
            _logger.LogWarning("Acceso a internet del chat está deshabilitado en configuración");
        }
    }

    /// <summary>
    /// Ejecuta una búsqueda web y retorna los resultados.
    /// </summary>
    public async Task<string> ExecuteAsync(string argumentsJson, CancellationToken cancellationToken = default)
    {
        try
        {
            // Verificar si el acceso a internet está habilitado
            var enabled = _configuration.GetValue<bool>("ChatAI:InternetAccess:Enabled", true);
            if (!enabled)
            {
                return JsonSerializer.Serialize(new
                {
                    error = "El acceso a internet del chat está deshabilitado en la configuración",
                    results = (object?)null
                });
            }

            // Parsear argumentos JSON
            var args = JsonSerializer.Deserialize<SearchArguments>(argumentsJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (args == null || string.IsNullOrWhiteSpace(args.Query))
            {
                return JsonSerializer.Serialize(new
                {
                    error = "La consulta de búsqueda no puede estar vacía",
                    results = (object?)null
                });
            }

            _logger.LogInformation("Ejecutando búsqueda web con proveedor {Provider}: {Query}", _provider, args.Query);

            // Ejecutar búsqueda según el proveedor configurado
            var results = _provider.ToLowerInvariant() switch
            {
                "serpapi" => await SearchWithSerpAPIAsync(args.Query, cancellationToken),
                "bing" => await SearchWithBingAsync(args.Query, cancellationToken),
                "duckduckgo" => await SearchWithDuckDuckGoAsync(args.Query, cancellationToken),
                _ => await SearchWithDuckDuckGoAsync(args.Query, cancellationToken) // Por defecto DuckDuckGo
            };

            return JsonSerializer.Serialize(new
            {
                success = true,
                query = args.Query,
                provider = _provider,
                results = results
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ejecutando búsqueda web");
            return JsonSerializer.Serialize(new
            {
                error = $"Error al realizar la búsqueda: {ex.Message}",
                results = (object?)null
            });
        }
    }

    private async Task<List<SearchResult>> SearchWithSerpAPIAsync(string query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new InvalidOperationException("SerpAPI requiere una API key configurada en ChatAI:WebSearch:ApiKey");
        }

        var searchUrl = $"https://serpapi.com/search.json?q={Uri.EscapeDataString(query)}&api_key={_apiKey}";
        var response = await _httpClient.GetAsync(searchUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var doc = JsonDocument.Parse(json);

        var results = new List<SearchResult>();
        if (doc.RootElement.TryGetProperty("organic_results", out var organicResults))
        {
            foreach (var item in organicResults.EnumerateArray().Take(5))
            {
                results.Add(new SearchResult
                {
                    Title = item.TryGetProperty("title", out var title) ? title.GetString() ?? "" : "",
                    Url = item.TryGetProperty("link", out var link) ? link.GetString() ?? "" : "",
                    Snippet = item.TryGetProperty("snippet", out var snippet) ? snippet.GetString() ?? "" : ""
                });
            }
        }

        return results;
    }

    private async Task<List<SearchResult>> SearchWithBingAsync(string query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new InvalidOperationException("Bing Search API requiere una API key configurada en ChatAI:WebSearch:ApiKey");
        }

        var searchUrl = $"https://api.bing.microsoft.com/v7.0/search?q={Uri.EscapeDataString(query)}&count=5";
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _apiKey);

        var response = await _httpClient.GetAsync(searchUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var doc = JsonDocument.Parse(json);

        var results = new List<SearchResult>();
        if (doc.RootElement.TryGetProperty("webPages", out var webPages) &&
            webPages.TryGetProperty("value", out var value))
        {
            foreach (var item in value.EnumerateArray())
            {
                results.Add(new SearchResult
                {
                    Title = item.TryGetProperty("name", out var title) ? title.GetString() ?? "" : "",
                    Url = item.TryGetProperty("url", out var resultUrl) ? resultUrl.GetString() ?? "" : "",
                    Snippet = item.TryGetProperty("snippet", out var snippet) ? snippet.GetString() ?? "" : ""
                });
            }
        }

        return results;
    }

    private async Task<List<SearchResult>> SearchWithDuckDuckGoAsync(string query, CancellationToken cancellationToken)
    {
        // DuckDuckGo Instant Answer API (gratis, limitado)
        // Nota: Esta es una implementación básica. Para búsquedas más completas,
        // se recomienda usar la librería DuckDuckGoSharp o SerpAPI
        try
        {
            var url = $"https://api.duckduckgo.com/?q={Uri.EscapeDataString(query)}&format=json&no_html=1&skip_disambig=1";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var doc = JsonDocument.Parse(json);

            var results = new List<SearchResult>();

            // DuckDuckGo Instant Answer API retorna resultados limitados
            if (doc.RootElement.TryGetProperty("AbstractText", out var abstractText) && !string.IsNullOrWhiteSpace(abstractText.GetString()))
            {
                results.Add(new SearchResult
                {
                    Title = doc.RootElement.TryGetProperty("Heading", out var heading) ? heading.GetString() ?? "" : "Resultado",
                    Url = doc.RootElement.TryGetProperty("AbstractURL", out var abstractUrl) ? abstractUrl.GetString() ?? "" : "",
                    Snippet = abstractText.GetString() ?? ""
                });
            }

            // Agregar resultados relacionados si existen
            if (doc.RootElement.TryGetProperty("RelatedTopics", out var relatedTopics))
            {
                foreach (var topic in relatedTopics.EnumerateArray().Take(4))
                {
                    if (topic.TryGetProperty("Text", out var text) && !string.IsNullOrWhiteSpace(text.GetString()))
                    {
                        var firstUrlValue = topic.TryGetProperty("FirstURL", out var firstUrlProp) 
                            ? firstUrlProp.GetString() ?? "" 
                            : "";
                        results.Add(new SearchResult
                        {
                            Title = !string.IsNullOrEmpty(firstUrlValue) 
                                ? firstUrlValue.Split('/').LastOrDefault() ?? "Resultado" 
                                : "Resultado",
                            Url = firstUrlValue,
                            Snippet = text.GetString() ?? ""
                        });
                    }
                }
            }

            // Si no hay resultados, retornar mensaje informativo
            if (results.Count == 0)
            {
                _logger.LogWarning("DuckDuckGo no retornó resultados. Considera usar SerpAPI o Bing para búsquedas más completas.");
                results.Add(new SearchResult
                {
                    Title = "Búsqueda sin resultados",
                    Url = "",
                    Snippet = "No se encontraron resultados con DuckDuckGo. Para búsquedas más completas, configura SerpAPI o Bing Search API."
                });
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error con DuckDuckGo, retornando resultado vacío");
            return new List<SearchResult>
            {
                new SearchResult
                {
                    Title = "Error en búsqueda",
                    Url = "",
                    Snippet = $"Error al buscar con DuckDuckGo: {ex.Message}. Considera configurar SerpAPI o Bing Search API para mejores resultados."
                }
            };
        }
    }

    private class SearchArguments
    {
        public string Query { get; set; } = string.Empty;
    }

    private class SearchResult
    {
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Snippet { get; set; } = string.Empty;
    }
}

