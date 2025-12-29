namespace FresiaFlow.Domain.Tasks;

/// <summary>
/// Representa una tarea diaria o pendiente del sistema.
/// </summary>
public class TaskItem
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public TaskPriority Priority { get; private set; }
    public bool IsCompleted { get; private set; }
    public bool IsPinned { get; private set; }
    public DateTime? DueDate { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public Guid? RelatedInvoiceId { get; private set; }
    public Guid? RelatedTransactionId { get; private set; }

    private TaskItem() { } // EF Core

    public TaskItem(
        string title,
        string? description = null,
        TaskPriority priority = TaskPriority.Medium,
        DateTime? dueDate = null)
    {
        Id = Guid.NewGuid();
        Title = title;
        Description = description;
        Priority = priority;
        IsCompleted = false;
        DueDate = dueDate;
        CreatedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        if (IsCompleted)
            return;

        IsCompleted = true;
        CompletedAt = DateTime.UtcNow;
    }

    public void Uncomplete()
    {
        IsCompleted = false;
        CompletedAt = null;
    }

    public void UpdatePriority(TaskPriority priority)
    {
        Priority = priority;
    }

    public void Pin()
    {
        IsPinned = true;
    }

    public void Unpin()
    {
        IsPinned = false;
    }

    public void TogglePin()
    {
        IsPinned = !IsPinned;
    }

    public void LinkToInvoice(Guid invoiceId)
    {
        RelatedInvoiceId = invoiceId;
    }

    public void LinkToTransaction(Guid transactionId)
    {
        RelatedTransactionId = transactionId;
    }
}

