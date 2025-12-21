namespace FresiaFlow.Adapters.Inbound.Api.Dtos;

/// <summary>
/// DTO para marcar una factura como pagada.
/// </summary>
public class MarkAsPaidDto
{
    public Guid TransactionId { get; set; }
}

