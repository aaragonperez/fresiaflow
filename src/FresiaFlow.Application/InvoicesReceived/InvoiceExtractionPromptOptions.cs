namespace FresiaFlow.Application.InvoicesReceived;

/// <summary>
/// Opciones de configuración para los prompts de extracción de facturas.
/// Permite modificar los prompts desde appsettings.json sin cambiar código.
/// </summary>
public class InvoiceExtractionPromptOptions
{
    public const string SectionName = "InvoiceExtraction:Prompts";

    /// <summary>
    /// Prompt del sistema para extracción de facturas.
    /// </summary>
    public string SystemMessage { get; set; } = "Eres un asistente experto en extracción de datos de facturas. Respondes únicamente con JSON válido.";

    /// <summary>
    /// Template del prompt de usuario para extracción básica (UploadInvoiceUseCase).
    /// Usa {0} como placeholder para el texto de la factura.
    /// </summary>
    public string BasicExtractionTemplate { get; set; } = @"Extrae los siguientes datos de la factura y devuélvelos en formato JSON estricto:

{{
  ""InvoiceNumber"": ""string"",
  ""IssueDate"": ""YYYY-MM-DD"",
  ""DueDate"": ""YYYY-MM-DD"" o null,
  ""Amount"": número decimal (sin símbolos de moneda),
  ""SupplierName"": ""string"",
  ""Confidence"": número decimal entre 0 y 1
}}

IMPORTANTE:
- InvoiceNumber: número de factura como string
- IssueDate: fecha en formato ISO (YYYY-MM-DD)
- DueDate: fecha en formato ISO (YYYY-MM-DD) o null si no existe
- Amount: solo el número decimal, sin símbolos de moneda ni espacios (ej: 43.19, no ""43,19 €"")
- SupplierName: nombre del proveedor
- Confidence: nivel de confianza de la extracción (0.0 a 1.0)

FACTURA:
{0}";

    /// <summary>
    /// Template del prompt de usuario para extracción completa (InvoiceExtractionService).
    /// Usa {0} como placeholder para el texto de la factura.
    /// </summary>
    public string CompleteExtractionTemplate { get; set; } = @"Eres un asistente experto en análisis de facturas. Extrae la siguiente información de esta factura y devuélvela en formato JSON estricto.

FACTURA:
{0}

Devuelve ÚNICAMENTE un objeto JSON válido con esta estructura exacta (sin texto adicional):
{{
  ""invoiceNumber"": ""string"",
  ""supplierName"": ""string"",
  ""supplierTaxId"": ""string o null"",
  ""issueDate"": ""YYYY-MM-DD"",
  ""dueDate"": ""YYYY-MM-DD o null"",
  ""totalAmount"": number,
  ""taxAmount"": number o null,
  ""subtotalAmount"": number o null,
  ""currency"": ""EUR"",
  ""lines"": [
    {{
      ""lineNumber"": 1,
      ""description"": ""string"",
      ""quantity"": number,
      ""unitPrice"": number,
      ""taxRate"": number o null,
      ""lineTotal"": number
    }}
  ]
}}

Reglas:
- Si no encuentras un campo, usa null para opcionales o un valor vacío/cero apropiado
- Las fechas deben estar en formato ISO (YYYY-MM-DD)
- Los números decimales deben usar punto como separador
- El currency por defecto es EUR si no se especifica
- Si no hay líneas detalladas, devuelve array vacío
- Devuelve SOLO el JSON, sin markdown ni texto adicional";
}

