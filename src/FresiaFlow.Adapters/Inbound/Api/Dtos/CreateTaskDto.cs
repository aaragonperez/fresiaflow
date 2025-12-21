using System.ComponentModel.DataAnnotations;

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
    /// Prioridad de la tarea. Valores válidos: Low (0), Medium (1), High (2), Urgent (3).
    /// Por defecto es Medium (1).
    /// </summary>
    [Required(ErrorMessage = "La prioridad es requerida.")]
    [Range(0, 3, ErrorMessage = "La prioridad debe estar entre 0 (Low) y 3 (Urgent).")]
    public int Priority { get; set; } = 1;
}

