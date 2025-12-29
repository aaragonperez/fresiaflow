using FresiaFlow.Application.Ports.Outbound;
using FresiaFlow.Application.UseCases;
using FresiaFlow.Domain.Banking;
using FresiaFlow.Domain.InvoicesReceived;
using FresiaFlow.Domain.Shared;
using FresiaFlow.Domain.Tasks;
using FluentAssertions;
using Moq;

namespace FresiaFlow.Tests.Application;

public class DashboardUseCaseTests
{
    private readonly Mock<IInvoiceReceivedRepository> _invoiceRepo = new();
    private readonly Mock<IBankAccountRepository> _bankAccountRepo = new();
    private readonly Mock<IBankTransactionRepository> _bankTxRepo = new();
    private readonly Mock<ITaskRepository> _taskRepo = new();
    private readonly DashboardUseCase _useCase;

    public DashboardUseCaseTests()
    {
        _useCase = new DashboardUseCase(
            _invoiceRepo.Object,
            _bankAccountRepo.Object,
            _bankTxRepo.Object,
            _taskRepo.Object);
    }

    [Fact]
    public async Task GetTasksAsync_ShouldIncludeDynamicInvoiceTasks()
    {
        var invoiceWithTask = BuildInvoice("INV-1", "<desconocido>", 0.4m);
        var invoiceWithoutTask = BuildInvoice("INV-2", "<desconocido>", 0.3m);

        _invoiceRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { invoiceWithTask, invoiceWithoutTask });

        // existing task linked to INV-1, so no dynamic task should be created for it
        var existingTask = new TaskItem("existing", null, TaskPriority.Medium);
        existingTask.LinkToInvoice(invoiceWithTask.Id);
        _taskRepo.Setup(r => r.GetPendingTasksAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TaskItem> { existingTask });

        var result = await _useCase.GetTasksAsync(CancellationToken.None);

        result.Tasks.Should().HaveCount(2); // existing + dynamic for INV-2
        result.Tasks.Any(t => t.Metadata?["invoiceId"]?.Equals(invoiceWithoutTask.Id) == true)
            .Should().BeTrue();
    }

    [Fact]
    public async Task GetAlertsAsync_ShouldIncludeLowConfidenceAndLowBalance()
    {
        var lowConfidence = BuildInvoice("INV-3", "Proveedor", 0.45m);
        _invoiceRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InvoiceReceived> { lowConfidence });

        var account = new BankAccount("123", "Bank", "checking");
        _bankAccountRepo.Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BankAccount> { account });

        var txs = new List<BankTransaction>
        {
            new BankTransaction(account.Id, DateTime.UtcNow.AddDays(-1), new Money(50, "EUR"), "low"),
            new BankTransaction(account.Id, DateTime.UtcNow, new Money(20, "EUR"), "lower")
        };
        _bankTxRepo.Setup(r => r.GetByAccountIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(txs);

        var alerts = (await _useCase.GetAlertsAsync(CancellationToken.None)).ToList();

        alerts.Should().Contain(a => a.Type == "system"); // low confidence invoice
        alerts.Should().Contain(a => a.Type == "low_balance"); // low balance
    }

    [Fact]
    public async Task GetBankBalancesAsync_ShouldAggregateBalance()
    {
        var account = new BankAccount("123", "Bank", "checking");
        _bankAccountRepo.Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BankAccount> { account });

        var txs = new[]
        {
            new BankTransaction(account.Id, DateTime.UtcNow, new Money(100, "EUR"), "in"),
            new BankTransaction(account.Id, DateTime.UtcNow, new Money(-40, "EUR"), "out")
        };
        _bankTxRepo.Setup(r => r.GetByAccountIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(txs.ToList());

        var summary = await _useCase.GetBankBalancesAsync(CancellationToken.None);

        summary.TotalBalance.Should().Be(60);
        summary.Banks.Should().ContainSingle();
    }

    private static InvoiceReceived BuildInvoice(string number, string supplier, decimal confidence)
    {
        var invoice = new InvoiceReceived(
            number,
            supplier,
            DateTime.UtcNow.Date,
            DateTime.UtcNow.Date,
            new Money(100, "EUR"),
            new Money(121, "EUR"),
            "EUR",
            InvoiceOrigin.ManualUpload,
            "file.pdf");
        invoice.SetExtractionConfidence(confidence);
        return invoice;
    }
}

