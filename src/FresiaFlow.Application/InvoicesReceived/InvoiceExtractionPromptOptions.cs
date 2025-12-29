namespace FresiaFlow.Application.InvoicesReceived;

/// <summary>
/// Opciones de configuración para los prompts de extracción de facturas.
/// Permite modificar los prompts desde appsettings.json sin cambiar código.
/// </summary>
public class InvoiceExtractionPromptOptions
{
    public const string SectionName = "InvoiceExtraction:Prompts";

    /// <summary>
    /// Lista de nombres de empresas propias que deben ser excluidas como proveedores.
    /// </summary>
    public List<string> OwnCompanyNames { get; set; } = new() { "FRESIA SOFTWARE SOLUTIONS", "Fresia Software Solutions", "Fresia Software" };

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

IMPORTANTE - EXCLUSIÓN DE EMPRESAS PROPIAS:
Las siguientes empresas son PROPIAS y NO deben ser consideradas como proveedores:
{1}

Si el proveedor de la factura coincide con alguna de estas empresas, NO proceses esta factura como factura recibida. En su lugar, devuelve null para supplierName o indica claramente que es una factura propia.

CRÍTICO - EXTRACCIÓN DEL PROVEEDOR:
- El PROVEEDOR (supplierName) es la empresa que EMITE la factura (quien vende/presta el servicio)
- El proveedor aparece típicamente en la parte superior de la factura, en la sección ""EMISOR"", ""DE:"", ""FROM:"", o en el encabezado
- El proveedor NO es el cliente (quien recibe la factura)
- El proveedor NO es tu empresa propia (listada arriba)
- SIEMPRE debes extraer el nombre completo del proveedor, incluso si es parcialmente visible
- Si no puedes identificar claramente al proveedor, usa el nombre que aparezca en el campo de emisor/vendedor de la factura
- NUNCA dejes supplierName vacío o null a menos que sea una empresa propia

Devuelve ÚNICAMENTE un objeto JSON válido con esta estructura exacta (sin texto adicional):
{{
  ""invoiceNumber"": ""string"",
  ""supplierName"": ""string"",
  ""supplierTaxId"": ""string o null"",
  ""issueDate"": ""YYYY-MM-DD"",
  ""dueDate"": ""YYYY-MM-DD o null"",
  ""totalAmount"": number,
  ""taxAmount"": number o null,
  ""taxRate"": number o null,
  ""irpfAmount"": number o null,
  ""irpfRate"": number o null,
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
- supplierName es OBLIGATORIO y debe contener el nombre de la empresa que emite la factura
- Si no encuentras un campo, usa null para opcionales o un valor vacío/cero apropiado
- Las fechas deben estar en formato ISO (YYYY-MM-DD)
- Los números decimales deben usar punto como separador
- El currency por defecto es EUR si no se especifica
- Si no hay líneas detalladas, devuelve array vacío
- NUNCA uses como supplierName ninguna de las empresas propias listadas arriba
- taxAmount es el importe de IVA (Impuesto sobre el Valor Añadido)
- taxRate es el porcentaje de IVA (ej: 21, 10, 4)
- irpfAmount es la retención de IRPF (Impuesto sobre la Renta) que se RESTA del total - común en facturas de autónomos/profesionales
- irpfRate es el porcentaje de IRPF (ej: 15, 7)
- El totalAmount debe ser: subtotalAmount + taxAmount - irpfAmount
- Devuelve SOLO el JSON, sin markdown ni texto adicional";
}

