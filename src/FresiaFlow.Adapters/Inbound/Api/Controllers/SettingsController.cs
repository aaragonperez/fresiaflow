using FresiaFlow.Application.InvoicesReceived;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace FresiaFlow.Adapters.Inbound.Api.Controllers;

/// <summary>
/// Controlador para gestión de configuración de la aplicación.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly InvoiceExtractionPromptOptions _promptOptions;
    private readonly IConfiguration _configuration;

    public SettingsController(
        IOptions<InvoiceExtractionPromptOptions> promptOptions,
        IConfiguration configuration)
    {
        _promptOptions = promptOptions.Value;
        _configuration = configuration;
    }

    /// <summary>
    /// Obtiene la configuración de empresas propias.
    /// </summary>
    [HttpGet("own-companies")]
    public IActionResult GetOwnCompanies()
    {
        return Ok(new
        {
            ownCompanyNames = _promptOptions.OwnCompanyNames ?? new List<string>()
        });
    }

    /// <summary>
    /// Actualiza la configuración de empresas propias.
    /// Nota: En producción, esto debería persistirse en base de datos.
    /// Por ahora, solo devuelve la configuración actualizada sin persistir.
    /// </summary>
    [HttpPost("own-companies")]
    public IActionResult UpdateOwnCompanies([FromBody] UpdateOwnCompaniesRequest request)
    {
        if (request.OwnCompanyNames == null)
        {
            return BadRequest(new { error = "La lista de empresas propias no puede ser null." });
        }

        // Nota: En una implementación completa, esto debería guardarse en base de datos
        // Por ahora, solo validamos y devolvemos
        var cleanedNames = request.OwnCompanyNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .Distinct()
            .ToList();

        return Ok(new
        {
            message = "Configuración actualizada (requiere reinicio del servidor para aplicar cambios)",
            ownCompanyNames = cleanedNames
        });
    }
}

/// <summary>
/// Request para actualizar empresas propias.
/// </summary>
public class UpdateOwnCompaniesRequest
{
    public List<string> OwnCompanyNames { get; set; } = new();
}

