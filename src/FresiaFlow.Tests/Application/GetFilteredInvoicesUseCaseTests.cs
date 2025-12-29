using FresiaFlow.Application.Ports.Inbound;
using FresiaFlow.Application.Ports.Outbound;
using FresiaFlow.Application.UseCases;
using FresiaFlow.Domain.InvoicesReceived;
using FresiaFlow.Domain.Shared;
using FluentAssertions;
using Moq;

namespace FresiaFlow.Tests.Application;

public class GetFilteredInvoicesUseCaseTests
{
    private readonly Mock<IInvoiceReceivedRepository> _repo = new();
    private readonly IGetFilteredInvoicesUseCase _useCase;

    public GetFilteredInvoicesUseCaseTests()
    {
        _useCase = new GetFilteredInvoicesUseCase(_repo.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPassFiltersToRepository()
    {
        // Arrange
        var expected = new List<InvoiceReceived>
        {
            new InvoiceReceived(
                "INV-1",
                "ACME",
                DateTime.UtcNow,
                DateTime.UtcNow,
                new Money(100, "EUR"),
                new Money(121, "EUR"),
                "EUR",
                InvoiceOrigin.ManualUpload,
                "file.pdf")
        };
        _repo.Setup(r => r.GetFilteredAsync(2024, 1, "ACME", PaymentType.Bank, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _useCase.ExecuteAsync(2024, 1, "ACME", PaymentType.Bank, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expected);
        _repo.Verify(r => r.GetFilteredAsync(2024, 1, "ACME", PaymentType.Bank, It.IsAny<CancellationToken>()), Times.Once);
    }
}

