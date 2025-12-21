using Microsoft.AspNetCore.Http;

namespace FresiaFlow.Adapters.Inbound.Api.Dtos;

/// <summary>
/// DTO para subir una factura.
/// </summary>
public class UploadInvoiceDto
{
    public IFormFile File { get; set; } = null!;
}

