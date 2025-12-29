namespace FresiaFlow.Application.InvoicesReceived;

/// <summary>
/// Utilidad para normalizar y validar CIFs/NIFs españoles extraídos de facturas.
/// </summary>
public static class TaxIdNormalizer
{
    /// <summary>
    /// CIF de Fresia que nunca debe ser usado como proveedor.
    /// </summary>
    private const string FresiaTaxId = "B87392700";

    /// <summary>
    /// Normaliza un CIF/NIF eliminando espacios, guiones, puntos y convirtiendo a mayúsculas.
    /// </summary>
    /// <param name="taxId">CIF/NIF a normalizar</param>
    /// <returns>CIF/NIF normalizado o null si es inválido o es el de Fresia</returns>
    public static string? Normalize(string? taxId)
    {
        if (string.IsNullOrWhiteSpace(taxId))
            return null;

        // Normalizar: eliminar espacios, guiones, puntos y convertir a mayúsculas
        var normalized = taxId
            .Replace(" ", "")
            .Replace("-", "")
            .Replace(".", "")
            .Replace(",", "")
            .ToUpperInvariant()
            .Trim();

        // Validar que no esté vacío después de normalizar
        if (string.IsNullOrWhiteSpace(normalized))
            return null;

        // Validar que no sea el CIF de Fresia (cliente, no proveedor)
        if (normalized == FresiaTaxId)
            return null;

        // Validar formato básico: debe tener entre 9 y 10 caracteres
        // CIF: Letra + 8 dígitos = 9 caracteres
        // NIF: 8 dígitos + letra = 9 caracteres
        // Algunos formatos pueden tener 10 caracteres con separadores que ya eliminamos
        if (normalized.Length < 8 || normalized.Length > 10)
            return null;

        // Validar que tenga al menos una letra y dígitos
        var hasLetter = normalized.Any(char.IsLetter);
        var hasDigit = normalized.Any(char.IsDigit);

        if (!hasLetter || !hasDigit)
            return null;

        return normalized;
    }

    /// <summary>
    /// Valida si un CIF/NIF es válido y no es el de Fresia.
    /// </summary>
    /// <param name="taxId">CIF/NIF a validar</param>
    /// <returns>true si es válido y no es el de Fresia</returns>
    public static bool IsValid(string? taxId)
    {
        var normalized = Normalize(taxId);
        return !string.IsNullOrWhiteSpace(normalized);
    }

    /// <summary>
    /// Valida si un CIF/NIF es el de Fresia (cliente).
    /// </summary>
    /// <param name="taxId">CIF/NIF a validar</param>
    /// <returns>true si es el CIF de Fresia</returns>
    public static bool IsFresiaTaxId(string? taxId)
    {
        if (string.IsNullOrWhiteSpace(taxId))
            return false;

        var normalized = taxId
            .Replace(" ", "")
            .Replace("-", "")
            .Replace(".", "")
            .Replace(",", "")
            .ToUpperInvariant()
            .Trim();

        return normalized == FresiaTaxId;
    }
}

