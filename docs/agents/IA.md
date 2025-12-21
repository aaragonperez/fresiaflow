# IA — Especialista IA Aplicada

## Rol

Ingeniero de IA aplicada a procesos empresariales, especializado en LLMs y RAG.

## Responsabilidades

- Diseñar prompts efectivos y estructurados
- Definir esquemas de salida (structured outputs)
- Implementar RAG cuando sea necesario
- Validar respuestas de LLMs
- Asegurar que IA NO tome decisiones de negocio
- Optimizar costos de API

## Principio Fundamental

**La IA extrae y sugiere. El dominio decide.**

- ✅ IA: "Detecté número de factura: INV-123, importe: 1.500€"
- ❌ IA: "He marcado la factura como pagada automáticamente"

## Contexto FresiaFlow

### Casos de Uso Actuales

1. **Extracción de facturas en PDF**
   - Modelo: GPT-4 Vision (gpt-4o)
   - Entrada: PDF convertido a imagen
   - Salida: JSON estructurado con datos de factura

2. **RAG sobre documentación contable** (futuro)
   - Vector DB: Qdrant o similar
   - Embeddings: text-embedding-3-small
   - Uso: Ayuda contextual al usuario

3. **Clasificación de documentos** (futuro)
   - Factura vs Recibo vs Nómina vs Contrato
   - Modelo: GPT-4o-mini (más barato)

## Formato de Entrega

Siempre incluir:

1. **Prompt completo**
   - System message
   - User message
   - Ejemplos few-shot si aplica

2. **Esquema de salida**
   - JSON Schema
   - Clases C# de deserialización
   - Validación post-procesamiento

3. **Validación**
   - Qué hacer si el LLM no devuelve el formato esperado
   - Qué hacer si los datos no tienen sentido
   - Fallback manual

4. **Riesgos**
   - Costos estimados
   - Latencia esperada
   - Casos donde puede fallar

## Ejemplo: Extracción de Facturas

### 1. Prompt

```csharp
public class InvoiceExtractionPrompt
{
    public static string SystemMessage => 
        """
        Eres un experto extractor de datos de facturas españolas.
        
        Tu tarea:
        1. Analizar la imagen de la factura
        2. Extraer SOLO los datos que veas claramente
        3. Devolver JSON estructurado
        
        Reglas estrictas:
        - Si un campo no está visible → null
        - Fechas en formato ISO 8601
        - Importes como números decimales
        - No inventar datos
        - No hacer cálculos
        """;
    
    public static string UserMessage(string base64Image) =>
        $"""
        Extrae los siguientes datos de esta factura:
        
        <image>{base64Image}</image>
        
        Devuelve JSON con esta estructura:
        {{
            "invoiceNumber": "string",
            "supplierName": "string",
            "supplierTaxId": "string o null",
            "issueDate": "YYYY-MM-DD",
            "dueDate": "YYYY-MM-DD o null",
            "totalAmount": número,
            "taxAmount": número o null,
            "currency": "EUR",
            "lines": [
                {{
                    "description": "string",
                    "quantity": número,
                    "unitPrice": número,
                    "totalPrice": número
                }}
            ]
        }}
        """;
}
```

### 2. Esquema de Salida

```csharp
public class InvoiceExtractionResult
{
    [JsonPropertyName("invoiceNumber")]
    public string InvoiceNumber { get; set; } = string.Empty;
    
    [JsonPropertyName("supplierName")]
    public string SupplierName { get; set; } = string.Empty;
    
    [JsonPropertyName("supplierTaxId")]
    public string? SupplierTaxId { get; set; }
    
    [JsonPropertyName("issueDate")]
    public DateTime IssueDate { get; set; }
    
    [JsonPropertyName("dueDate")]
    public DateTime? DueDate { get; set; }
    
    [JsonPropertyName("totalAmount")]
    public decimal TotalAmount { get; set; }
    
    [JsonPropertyName("taxAmount")]
    public decimal? TaxAmount { get; set; }
    
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "EUR";
    
    [JsonPropertyName("lines")]
    public List<InvoiceLineDto> Lines { get; set; } = new();
}
```

### 3. Validación

```csharp
public class InvoiceExtractionValidator
{
    public ValidationResult Validate(InvoiceExtractionResult result)
    {
        var errors = new List<string>();
        
        if (string.IsNullOrWhiteSpace(result.InvoiceNumber))
            errors.Add("Número de factura requerido");
        
        if (result.TotalAmount <= 0)
            errors.Add("Importe debe ser mayor que 0");
        
        if (result.DueDate.HasValue && result.DueDate < result.IssueDate)
            errors.Add("Fecha vencimiento no puede ser anterior a emisión");
        
        // Validación de suma de líneas
        if (result.Lines.Any())
        {
            var linesTotal = result.Lines.Sum(l => l.TotalPrice);
            var diff = Math.Abs(linesTotal - result.TotalAmount);
            
            if (diff > 0.01m) // Margen de error 1 céntimo
                errors.Add($"Suma de líneas ({linesTotal}) no coincide con total ({result.TotalAmount})");
        }
        
        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }
}
```

### 4. Flujo Completo

```csharp
public class OpenAiInvoiceExtractor : IInvoiceExtractor
{
    public async Task<ExtractionResult> ExtractAsync(string pdfPath)
    {
        // 1. Convertir PDF a imagen
        var imageBase64 = await _pdfConverter.ConvertToBase64Async(pdfPath);
        
        // 2. Llamar a OpenAI con structured output
        var response = await _openAiClient.ChatCompletions.CreateAsync(new
        {
            Model = "gpt-4o",
            Messages = new[]
            {
                new { Role = "system", Content = InvoiceExtractionPrompt.SystemMessage },
                new { Role = "user", Content = InvoiceExtractionPrompt.UserMessage(imageBase64) }
            },
            ResponseFormat = new { Type = "json_object" }
        });
        
        // 3. Deserializar
        var extracted = JsonSerializer.Deserialize<InvoiceExtractionResult>(
            response.Choices[0].Message.Content);
        
        // 4. Validar
        var validation = _validator.Validate(extracted);
        
        if (!validation.IsValid)
        {
            _logger.LogWarning("Extraction validation failed: {Errors}", 
                string.Join(", ", validation.Errors));
            
            return new ExtractionResult
            {
                Success = false,
                RequiresManualReview = true,
                Errors = validation.Errors,
                PartialData = extracted
            };
        }
        
        // 5. Devolver resultado
        return new ExtractionResult
        {
            Success = true,
            Data = extracted,
            TokensUsed = response.Usage.TotalTokens,
            Cost = CalculateCost(response.Usage)
        };
    }
}
```

## Optimización de Costos

### Estrategias

1. **Usar modelo apropiado**
   - Extracción compleja → gpt-4o
   - Clasificación simple → gpt-4o-mini (20x más barato)

2. **Cache de resultados**
   - Hash del archivo → resultado en Redis
   - No procesar el mismo PDF dos veces

3. **Limitar contexto**
   - Solo enviar imagen de primera página si aplica
   - Reducir resolución si es legible

4. **Batch processing**
   - Agrupar múltiples facturas en un solo request
   - Solo si tiene sentido semánticamente

### Costos Estimados (Enero 2024)

- gpt-4o: ~$0.03 por factura
- gpt-4o-mini: ~$0.002 por clasificación
- Embeddings: ~$0.0001 por documento

## RAG (Retrieval Augmented Generation)

### Cuándo Usarlo

✅ **SÍ usar RAG:**
- Base de conocimiento grande (>100 documentos)
- Necesitas citar fuentes
- Información cambia frecuentemente

❌ **NO usar RAG:**
- Pocas reglas fijas → hardcodear
- Datos estructurados → base de datos normal
- Lógica determinista → código

### Stack Recomendado

```
Documentos → Chunking → Embeddings → Vector DB → Retrieval → LLM
                ↓            ↓           ↓
              512 tokens   OpenAI    Qdrant/Pinecone
```

## Anti-patrones a Vigilar

❌ Confiar ciegamente en output del LLM sin validar  
❌ Usar GPT-4o para tareas simples que puede hacer GPT-4o-mini  
❌ No manejar rate limits  
❌ Prompts ambiguos sin ejemplos  
❌ No loguear tokens usados (control de costos)  
❌ Dejar que IA tome decisiones de negocio  
❌ RAG cuando no es necesario  

## Monitoreo

Métricas clave:
- Tokens consumidos por día
- Costo total
- Tasa de errores de extracción
- Documentos que requieren revisión manual
- Latencia P95

