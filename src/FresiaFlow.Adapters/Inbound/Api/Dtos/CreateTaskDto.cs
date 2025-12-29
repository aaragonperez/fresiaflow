using System.ComponentModel.DataAnnotations;
using FresiaFlow.Domain.Tasks;

namespace FresiaFlow.Adapters.Inbound.Api.Dtos;

/// <summary>
/// DTO para crear una tarea.
/// </summary>
public class CreateTaskDto
{
    /// <summary>
    /// Título de la tarea. Requerido y con longitud máxima de 200 caracteres.
    /// </summary>
    [Required(ErrorMessage = "El título de la tarea es requerido.")]
    [StringLength(200, ErrorMessage = "El título no puede exceder 200 caracteres.")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Descripción opcional de la tarea.
    /// </summary>
    [StringLength(1000, ErrorMessage = "La descripción no puede exceder 1000 caracteres.")]
    public string? Description { get; set; }

    /// <summary>
    /// Prioridad de la tarea.
    /// </summary>
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    /// <summary>
    /// Fecha de vencimiento opcional.
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// ID de factura relacionada (opcional).
    /// </summary>
    public Guid? RelatedInvoiceId { get; set; }

    /// <summary>
    /// ID de transacción bancaria relacionada (opcional).
    /// </summary>
    public Guid? RelatedTransactionId { get; set; }
}

