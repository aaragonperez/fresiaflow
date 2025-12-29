using FresiaFlow.Application.Ports.Outbound;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace FresiaFlow.Adapters.Outbound.OpenAI;

/// <summary>
/// Adapter para integración con OpenAI API.
/// Aísla los detalles de implementación de OpenAI del dominio.
/// </summary>
public class OpenAIAdapter : IOpenAIClient, IChatAIClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;

    public OpenAIAdapter(HttpClient httpClient, string apiKey, string model = "gpt-4")
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
        _model = model;
    }

    public async Task<string> GetChatCompletionAsync(
        string systemPrompt,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        var requestBody = new
        {
            model = _model.Contains("gpt-4o") ? _model : "gpt-4o",
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userMessage }
            },
            temperature = 0.7,
            max_tokens = 2000
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

        var response = await _httpClient.PostAsync(
            "https://api.openai.com/v1/chat/completions",
            content,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"OpenAI API error ({response.StatusCode}): {errorContent}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        
        var openAiResponse = JsonSerializer.Deserialize<OpenAiChatResponse>(
            responseJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (openAiResponse?.Choices == null || openAiResponse.Choices.Length == 0)
        {
            throw new InvalidOperationException("OpenAI no devolvió ninguna respuesta válida");
        }

        return openAiResponse.Choices[0].Message?.Content?.Trim() ?? string.Empty;
    }

    public async Task<ToolCallResult> GetChatCompletionWithToolsAsync(
        string systemPrompt,
        string userMessage,
        List<ToolDefinition> availableTools,
        CancellationToken cancellationToken = default)
    {
        // Convertir ToolDefinition a formato OpenAI tools
        var tools = availableTools.Select(tool => new
        {
            type = "function",
            function = new
            {
                name = tool.Name,
                description = tool.Description,
                parameters = tool.Parameters
            }
        }).ToArray();

        // Construir mensajes
        var messages = new[]
        {
            new { role = "system", content = systemPrompt },
            new { role = "user", content = userMessage }
        };

        // Usar el modelo configurado, asegurándose de que soporte tool calling
        var modelForTools = _model.Contains("gpt-4o") ? _model : 
                           _model.Contains("turbo") ? _model : 
                           "gpt-4o-mini"; // Fallback a modelo que soporta tool calling

        var requestBody = new
        {
            model = modelForTools,
            messages = messages,
            tools = tools,
            tool_choice = "auto", // Dejar que OpenAI decida cuándo usar herramientas
            temperature = 0.7,
            max_tokens = 2000
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

        var response = await _httpClient.PostAsync(
            "https://api.openai.com/v1/chat/completions",
            content,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"OpenAI API error ({response.StatusCode}): {errorContent}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        
        var openAiResponse = JsonSerializer.Deserialize<OpenAiChatResponseWithTools>(
            responseJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (openAiResponse?.Choices == null || openAiResponse.Choices.Length == 0)
        {
            throw new InvalidOperationException("OpenAI no devolvió ninguna respuesta válida");
        }

        var choice = openAiResponse.Choices[0];
        var message = choice.Message;

        // Extraer contenido del mensaje
        var messageContent = message?.Content?.Trim() ?? string.Empty;

        // Extraer tool calls si existen
        var toolCalls = new List<ToolCall>();
        if (message?.ToolCalls != null && message.ToolCalls.Length > 0)
        {
            foreach (var toolCall in message.ToolCalls)
            {
                if (toolCall.Function != null)
                {
                    toolCalls.Add(new ToolCall(
                        toolCall.Function.Name ?? string.Empty,
                        toolCall.Function.Arguments ?? "{}"
                    ));
                }
            }
        }

        return new ToolCallResult(
            string.IsNullOrWhiteSpace(messageContent) ? null : messageContent,
            toolCalls
        );
    }

    public async Task<T> ExtractStructuredDataAsync<T>(
        string text,
        string schemaDescription,
        CancellationToken cancellationToken = default) where T : class
    {
        var prompt = $@"{schemaDescription}

Texto a analizar:
{text}

IMPORTANTE: Responde ÚNICAMENTE con un objeto JSON válido. No incluyas markdown, comentarios ni texto adicional. Solo el JSON.";

        // Solo usar response_format con modelos que lo soporten (gpt-4-turbo, gpt-3.5-turbo)
        // gpt-4 base no soporta response_format, causa error 400
        var supportsJsonMode = _model.Contains("turbo") || _model.Contains("gpt-4o");
        
        object requestBody;
        
        if (supportsJsonMode)
        {
            requestBody = new
            {
                model = _model,
                messages = new[]
                {
                    new { role = "system", content = "Eres un asistente experto en extracción de datos estructurados. Respondes ÚNICAMENTE con JSON válido, sin markdown ni texto adicional." },
                    new { role = "user", content = prompt }
                },
                response_format = new { type = "json_object" },
                temperature = 0.1,
                max_tokens = 2000
            };
        }
        else
        {
            requestBody = new
            {
                model = _model,
                messages = new[]
                {
                    new { role = "system", content = "Eres un asistente experto en extracción de datos estructurados. Respondes ÚNICAMENTE con JSON válido, sin markdown ni texto adicional." },
                    new { role = "user", content = prompt }
                },
                temperature = 0.1,
                max_tokens = 2000
            };
        }

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

        var response = await _httpClient.PostAsync(
            "https://api.openai.com/v1/chat/completions",
            content,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"OpenAI API error ({response.StatusCode}): {errorContent}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        
        if (string.IsNullOrEmpty(responseJson))
            throw new InvalidOperationException("OpenAI devolvió una respuesta vacía");
        
        var openAiResponse = JsonSerializer.Deserialize<OpenAiChatResponse>(
            responseJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (openAiResponse?.Choices == null || (openAiResponse?.Choices?.Length ?? 0) == 0)
        {
            throw new InvalidOperationException("OpenAI no devolvió ninguna respuesta válida");
        }

        var extractedJson = openAiResponse?.Choices?[0]?.Message?.Content?.Trim() ?? string.Empty;
        
        if (string.IsNullOrEmpty(extractedJson))
        {
            throw new InvalidOperationException("OpenAI devolvió una respuesta vacía");
        }
        
        // Limpiar markdown si existe
        if (extractedJson.StartsWith("```json"))
        {
            extractedJson = extractedJson.Substring(7);
        }
        if (extractedJson.StartsWith("```"))
        {
            extractedJson = extractedJson.Substring(3);
        }
        if (extractedJson.EndsWith("```"))
        {
            extractedJson = extractedJson.Substring(0, extractedJson.Length - 3);
        }
        extractedJson = extractedJson.Trim();

        T? result;
        try
        {
            result = JsonSerializer.Deserialize<T>(
                extractedJson ?? string.Empty,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Error deserializando JSON de OpenAI: {ex.Message}", ex);
        }

        if (result == null)
        {
            throw new InvalidOperationException("No se pudo deserializar la respuesta de OpenAI");
        }

        return result;
    }

    public async Task<T> ExtractStructuredDataFromPdfAsync<T>(
        string filePath,
        string schemaDescription,
        CancellationToken cancellationToken = default) where T : class
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"El archivo no existe: {filePath}");
        }

        var fileExtension = Path.GetExtension(filePath).ToLowerInvariant();
        var isImage = fileExtension == ".jpg" || fileExtension == ".jpeg" || fileExtension == ".png" || fileExtension == ".gif" || fileExtension == ".webp";
        
        if (isImage)
        {
            // Para imágenes, usar la API de vision directamente
            return await ExtractStructuredDataFromImageAsync<T>(filePath, schemaDescription, cancellationToken);
        }

        // Para PDFs, usar la API de archivos
        // 1. Subir el archivo PDF a OpenAI
        var fileId = await UploadFileToOpenAIAsync(filePath, cancellationToken);
        
        // #region agent log
        try { await System.IO.File.AppendAllTextAsync(@"c:\repo\FresiaFlow\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "B", location = "OpenAIAdapter.cs:188", message = "FileId obtenido", data = new { fileId = fileId ?? "NULL", fileIdLength = fileId?.Length ?? 0 }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n", cancellationToken); } catch { }
        // #endregion

        try
        {
            // 2. Usar el file_id para extraer datos estructurados
            var prompt = $@"{schemaDescription}

IMPORTANTE: Responde ÚNICAMENTE con un objeto JSON válido. No incluyas markdown, comentarios ni texto adicional. Solo el JSON.";

            // Usar gpt-4o que soporta archivos PDF
            var visionModel = _model.Contains("gpt-4o") ? _model : "gpt-4o";
            var supportsJsonMode = visionModel.Contains("turbo") || visionModel.Contains("gpt-4o");

            object requestBody;
            
            var userContent = new List<object>
            {
                new
                {
                    type = "file",
                    file = new { file_id = fileId }
                },
                new
                {
                    type = "text",
                    text = prompt
                }
            };
            
            // #region agent log
            try { await System.IO.File.AppendAllTextAsync(@"c:\repo\FresiaFlow\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "post-fix", hypothesisId = "A", location = "OpenAIAdapter.cs:207", message = "Estructura userContent después del fix", data = new { userContentType = userContent.GetType().Name, userContentCount = userContent.Count, firstItemType = userContent[0].GetType().Name, fileId = fileId }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n", cancellationToken); } catch { }
            // #endregion

            var messages = new List<object>
            {
                new
                {
                    role = "system",
                    content = "Eres un asistente experto en extracción de datos estructurados de facturas. Respondes ÚNICAMENTE con JSON válido, sin markdown ni texto adicional."
                },
                new
                {
                    role = "user",
                    content = userContent
                }
            };

            if (supportsJsonMode)
            {
                requestBody = new
                {
                    model = visionModel,
                    messages = messages,
                    response_format = new { type = "json_object" },
                    temperature = 0.1,
                    max_tokens = 2000
                };
            }
            else
            {
                requestBody = new
                {
                    model = visionModel,
                    messages = messages,
                    temperature = 0.1,
                    max_tokens = 2000
                };
            }

            var json = JsonSerializer.Serialize(requestBody);
            
            // #region agent log
            try { await System.IO.File.AppendAllTextAsync(@"c:\repo\FresiaFlow\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "post-fix", hypothesisId = "A,C", location = "OpenAIAdapter.cs:253", message = "JSON serializado después del fix", data = new { jsonLength = json.Length, jsonPreview = json.Length > 500 ? json.Substring(0, 500) + "..." : json, containsFileId = json.Contains(fileId ?? ""), containsFileType = json.Contains("\"type\":\"file\""), containsFileObject = json.Contains("\"file\":") }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n", cancellationToken); } catch { }
            // #endregion
            
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

            var response = await _httpClient.PostAsync(
                "https://api.openai.com/v1/chat/completions",
                content,
                cancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            
            // #region agent log
            try { await System.IO.File.AppendAllTextAsync(@"c:\repo\FresiaFlow\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "post-fix", hypothesisId = "A,C", location = "OpenAIAdapter.cs:275", message = "Respuesta completa de OpenAI", data = new { statusCode = (int)response.StatusCode, isSuccess = response.IsSuccessStatusCode, responseLength = responseContent?.Length ?? 0, responsePreview = responseContent?.Length > 1000 ? responseContent.Substring(0, 1000) + "..." : responseContent }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n", cancellationToken); } catch { }
            // #endregion

            if (!response.IsSuccessStatusCode)
            {
                // #region agent log
                try { await System.IO.File.AppendAllTextAsync(@"c:\repo\FresiaFlow\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "post-fix", hypothesisId = "A,C,D,E", location = "OpenAIAdapter.cs:282", message = "Error de OpenAI recibido", data = new { statusCode = (int)response.StatusCode, errorContent = responseContent, fileId = fileId }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n", cancellationToken); } catch { }
                // #endregion
                
                throw new InvalidOperationException(
                    $"OpenAI API error ({response.StatusCode}): {responseContent}");
            }

            var responseJson = responseContent;
            
            if (string.IsNullOrEmpty(responseJson))
                throw new InvalidOperationException("OpenAI devolvió una respuesta vacía");
            
            var openAiResponse = JsonSerializer.Deserialize<OpenAiChatResponse>(
                responseJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (openAiResponse?.Choices == null || (openAiResponse?.Choices?.Length ?? 0) == 0)
            {
                throw new InvalidOperationException("OpenAI no devolvió ninguna respuesta válida");
            }

            var extractedJson = openAiResponse?.Choices?[0]?.Message?.Content?.Trim() ?? string.Empty;
            
            if (string.IsNullOrEmpty(extractedJson))
            {
                throw new InvalidOperationException("OpenAI devolvió una respuesta vacía");
            }

            // Limpiar markdown si existe
            if (extractedJson.StartsWith("```json"))
            {
                extractedJson = extractedJson.Substring(7);
            }
            if (extractedJson.StartsWith("```"))
            {
                extractedJson = extractedJson.Substring(3);
            }
            if (extractedJson.EndsWith("```"))
            {
                extractedJson = extractedJson.Substring(0, extractedJson.Length - 3);
            }
            extractedJson = extractedJson.Trim();

            T? result;
            try
            {
                result = JsonSerializer.Deserialize<T>(
                    extractedJson ?? string.Empty,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Error deserializando JSON de OpenAI: {ex.Message}", ex);
            }

            if (result == null)
            {
                throw new InvalidOperationException("No se pudo deserializar la respuesta de OpenAI");
            }

            return result;
        }
        finally
        {
            // 3. Limpiar: eliminar el archivo de OpenAI
            if (!string.IsNullOrWhiteSpace(fileId))
            {
                await DeleteFileFromOpenAIAsync(fileId!, cancellationToken);
            }
        }
    }

    private async Task<string> UploadFileToOpenAIAsync(string filePath, CancellationToken cancellationToken)
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

        using var fileStream = File.OpenRead(filePath);
        using var content = new MultipartFormDataContent();
        
        var fileContent = new StreamContent(fileStream);
        
        // Determinar el tipo MIME según la extensión
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        var mimeType = extension switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "application/pdf"
        };
        
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType);
        
        content.Add(fileContent, "file", Path.GetFileName(filePath));
        content.Add(new StringContent("user_data"), "purpose");

        var response = await _httpClient.PostAsync(
            "https://api.openai.com/v1/files",
            content,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Error subiendo archivo a OpenAI ({response.StatusCode}): {errorContent}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        
        if (string.IsNullOrWhiteSpace(responseJson))
        {
            throw new InvalidOperationException("OpenAI devolvió una respuesta vacía al subir el archivo");
        }
        
        // #region agent log
        try { await System.IO.File.AppendAllTextAsync(@"c:\repo\FresiaFlow\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "B", location = "OpenAIAdapter.cs:360", message = "Respuesta de upload file", data = new { responseJson = responseJson, responseJsonLength = responseJson.Length }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n", cancellationToken); } catch { }
        // #endregion
        
        var fileResponse = JsonSerializer.Deserialize<OpenAiFileResponse>(
            responseJson!,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (fileResponse?.Id == null || string.IsNullOrWhiteSpace(fileResponse.Id))
        {
            // #region agent log
            try { await System.IO.File.AppendAllTextAsync(@"c:\repo\FresiaFlow\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "B", location = "OpenAIAdapter.cs:365", message = "FileId es null", data = new { fileResponseIsNull = fileResponse == null, fileIdIsNull = fileResponse?.Id == null }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n", cancellationToken); } catch { }
            // #endregion
            throw new InvalidOperationException("OpenAI no devolvió un file_id válido");
        }

        // #region agent log
        try { await System.IO.File.AppendAllTextAsync(@"c:\repo\FresiaFlow\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "B", location = "OpenAIAdapter.cs:369", message = "FileId extraído correctamente", data = new { fileId = fileResponse.Id, fileIdLength = fileResponse.Id.Length }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n", cancellationToken); } catch { }
        // #endregion

        return fileResponse.Id!;
    }

    private async Task<T> ExtractStructuredDataFromImageAsync<T>(
        string imagePath,
        string schemaDescription,
        CancellationToken cancellationToken = default) where T : class
    {
        // Convertir imagen a base64
        var imageBytes = await File.ReadAllBytesAsync(imagePath, cancellationToken);
        var base64Image = Convert.ToBase64String(imageBytes);
        
        // Determinar el tipo MIME
        var extension = Path.GetExtension(imagePath).ToLowerInvariant();
        var mimeType = extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "image/jpeg"
        };

        var prompt = $@"{schemaDescription}

IMPORTANTE: Responde ÚNICAMENTE con un objeto JSON válido. No incluyas markdown, comentarios ni texto adicional. Solo el JSON.";

        var userContent = new List<object>
        {
            new
            {
                type = "image_url",
                image_url = new
                {
                    url = $"data:{mimeType};base64,{base64Image}"
                }
            },
            new
            {
                type = "text",
                text = prompt
            }
        };

        var messages = new List<object>
        {
            new
            {
                role = "system",
                content = "Eres un asistente experto en extracción de datos estructurados de facturas. Respondes ÚNICAMENTE con JSON válido, sin markdown ni texto adicional."
            },
            new
            {
                role = "user",
                content = userContent
            }
        };

        var requestBody = new
        {
            model = "gpt-4o", // gpt-4o soporta vision
            messages = messages,
            response_format = new { type = "json_object" },
            temperature = 0.1,
            max_tokens = 2000
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

        var response = await _httpClient.PostAsync(
            "https://api.openai.com/v1/chat/completions",
            content,
            cancellationToken);

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"OpenAI API error ({response.StatusCode}): {responseContent}");
        }

        var openAiResponse = JsonSerializer.Deserialize<OpenAiChatResponse>(
            responseContent,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (openAiResponse?.Choices == null || openAiResponse.Choices.Length == 0)
        {
            throw new InvalidOperationException("OpenAI no devolvió ninguna respuesta válida");
        }

        var messageContent = openAiResponse.Choices[0].Message?.Content;
        if (string.IsNullOrWhiteSpace(messageContent))
        {
            throw new InvalidOperationException("OpenAI devolvió un mensaje vacío");
        }

        var extractedJson = messageContent!.Trim();

        // Limpiar markdown si existe
        if (extractedJson.StartsWith("```json"))
        {
            extractedJson = extractedJson.Replace("```json", "").Replace("```", "").Trim();
        }
        else if (extractedJson.StartsWith("```"))
        {
            extractedJson = extractedJson.Replace("```", "").Trim();
        }

        var result = JsonSerializer.Deserialize<T>(
            extractedJson ?? string.Empty,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (result == null)
        {
            throw new InvalidOperationException("No se pudo deserializar la respuesta de OpenAI");
        }

        return result;
    }

    private async Task DeleteFileFromOpenAIAsync(string fileId, CancellationToken cancellationToken)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

            var response = await _httpClient.DeleteAsync(
                $"https://api.openai.com/v1/files/{fileId}",
                cancellationToken);

            // No lanzar excepción si falla la eliminación, solo loguear
            if (!response.IsSuccessStatusCode)
            {
                // Log warning si es necesario
            }
        }
        catch
        {
            // Ignorar errores al eliminar el archivo
        }
    }

    private class OpenAiChatResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("choices")]
        public OpenAiChoice[]? Choices { get; set; }
    }

    private class OpenAiChoice
    {
        [System.Text.Json.Serialization.JsonPropertyName("message")]
        public OpenAiMessage? Message { get; set; }
    }

    private class OpenAiMessage
    {
        [System.Text.Json.Serialization.JsonPropertyName("content")]
        public string? Content { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("tool_calls")]
        public OpenAiToolCall[]? ToolCalls { get; set; }
    }

    private class OpenAiChatResponseWithTools
    {
        [System.Text.Json.Serialization.JsonPropertyName("choices")]
        public OpenAiChoiceWithTools[]? Choices { get; set; }
    }

    private class OpenAiChoiceWithTools
    {
        [System.Text.Json.Serialization.JsonPropertyName("message")]
        public OpenAiMessage? Message { get; set; }
    }

    private class OpenAiToolCall
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public string? Id { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("type")]
        public string? Type { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("function")]
        public OpenAiToolCallFunction? Function { get; set; }
    }

    private class OpenAiToolCallFunction
    {
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string? Name { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("arguments")]
        public string? Arguments { get; set; }
    }

    private class OpenAiFileResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public string? Id { get; set; }
    }

    private async Task<string> CallOpenAIAsync(object requestBody, CancellationToken cancellationToken)
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        // TODO: Parsear respuesta y extraer mensaje/tool calls

        return responseJson;
    }
}

