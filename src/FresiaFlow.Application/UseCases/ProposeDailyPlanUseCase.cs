using FresiaFlow.Application.Ports.Inbound;
using FresiaFlow.Application.Ports.Outbound;
using FresiaFlow.Application.AI;
using FresiaFlow.Domain.Tasks;
using FresiaFlow.Domain.Invoices;
using FresiaFlow.Domain.Banking;

namespace FresiaFlow.Application.UseCases;

/// <summary>
/// Caso de uso para generar un plan diario usando IA.
/// Orquesta la recopilación de contexto y la generación de tareas.
/// </summary>
public class ProposeDailyPlanUseCase : IProposeDailyPlanUseCase
{
    private readonly IFresiaFlowOrchestrator _aiOrchestrator;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IBankTransactionRepository _transactionRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly IVectorStore _vectorStore;

    public ProposeDailyPlanUseCase(
        IFresiaFlowOrchestrator aiOrchestrator,
        IInvoiceRepository invoiceRepository,
        IBankTransactionRepository transactionRepository,
        ITaskRepository taskRepository,
        IVectorStore vectorStore)
    {
        _aiOrchestrator = aiOrchestrator;
        _invoiceRepository = invoiceRepository;
        _transactionRepository = transactionRepository;
        _taskRepository = taskRepository;
        _vectorStore = vectorStore;
    }

    public async Task<DailyPlanResult> ExecuteAsync(ProposeDailyPlanCommand command, CancellationToken cancellationToken = default)
    {
        // 1. Recopilar contexto
        var context = await BuildContextAsync(command, cancellationToken);

        // 2. Buscar procedimientos relevantes en RAG
        var relevantProcedures = await _vectorStore.SearchSimilarAsync(
            $"Plan diario para {command.Date:yyyy-MM-dd}",
            topK: 3,
            cancellationToken);

        // 3. Generar plan usando IA
        var plan = await _aiOrchestrator.GenerateDailyPlanAsync(
            command.Date,
            context,
            relevantProcedures.Select(r => r.Content).ToList(),
            cancellationToken);

        // 4. Convertir a entidades TaskItem
        var tasks = plan.ProposedTasks.Select(t => new TaskItem(
            t.Title,
            t.Description,
            MapPriority(t.Priority),
            t.DueDate
        )).ToList();

        // 5. Persistir tareas
        foreach (var task in tasks)
        {
            await _taskRepository.AddAsync(task, cancellationToken);
        }

        return new DailyPlanResult(
            tasks,
            plan.Summary,
            plan.Recommendations
        );
    }

    private async Task<DailyPlanContext> BuildContextAsync(ProposeDailyPlanCommand command, CancellationToken cancellationToken)
    {
        var context = new DailyPlanContext
        {
            Date = command.Date
        };

        if (command.IncludePendingInvoices)
        {
            context.PendingInvoices = await _invoiceRepository.GetPendingInvoicesAsync(cancellationToken);
        }

        if (command.IncludeUnreconciledTransactions)
        {
            context.UnreconciledTransactions = await _transactionRepository.GetUnreconciledAsync(cancellationToken);
        }

        return context;
    }

    private static TaskPriority MapPriority(string priority)
    {
        return priority.ToLower() switch
        {
            "urgent" => TaskPriority.Urgent,
            "high" => TaskPriority.High,
            "low" => TaskPriority.Low,
            _ => TaskPriority.Medium
        };
    }

    private class DailyPlanContext
    {
        public DateTime Date { get; set; }
        public List<Domain.Invoices.Invoice> PendingInvoices { get; set; } = new();
        public List<Domain.Banking.BankTransaction> UnreconciledTransactions { get; set; } = new();
    }
}

