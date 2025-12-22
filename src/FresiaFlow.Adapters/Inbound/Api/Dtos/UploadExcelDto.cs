using Microsoft.AspNetCore.Http;

namespace FresiaFlow.Adapters.Inbound.Api.Dtos;

/// <summary>
/// DTO para subir un archivo Excel.
/// </summary>
public class UploadExcelDto
{
    public IFormFile File { get; set; } = null!;
}

