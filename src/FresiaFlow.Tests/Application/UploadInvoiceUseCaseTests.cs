using FresiaFlow.Application.InvoicesReceived;
using FresiaFlow.Application.Ports.Inbound;
using FresiaFlow.Application.Ports.Outbound;
using FresiaFlow.Application.UseCases;
using FresiaFlow.Domain.Invoices;
using FresiaFlow.Domain.Shared;
using Microsoft.Extensions.Options;
using Moq;
using FluentAssertions;

namespace FresiaFlow.Tests.Application;

public class UploadInvoiceUseCaseTests
{
    private readonly Mock<IInvoiceReceivedRepository> _invoiceRepositoryMock;
    private readonly Mock<IFileStorage> _fileStorageMock;
    private readonly Mock<IOpenAIClient> _openAIClientMock;
    private readonly InvoiceExtractionPromptOptions _promptOptions;
    private readonly UploadInvoiceUseCase _useCase;

    public UploadInvoiceUseCaseTests()
    {
        _invoiceRepositoryMock = new Mock<IInvoiceReceivedRepository>();
        _fileStorageMock = new Mock<IFileStorage>();
        _openAIClientMock = new Mock<IOpenAIClient>();
        
        _promptOptions = new InvoiceExtractionPromptOptions
        {
            BasicExtractionTemplate = "Extract invoice data from: {0}"
        };

        var optionsMock = new Mock<IOptions<InvoiceExtractionPromptOptions>>();
        optionsMock.Setup(x => x.Value).Returns(_promptOptions);

        _useCase = new UploadInvoiceUseCase(
            _invoiceRepositoryMock.Object,
            _fileStorageMock.Object,
            _openAIClientMock.Object,
            optionsMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyInvoiceNumber_ShouldThrowException()
    {
        // Arrange
        var fileStream = new MemoryStream();
        var fileName = "invoice.pdf";
        var contentType = "application/pdf";
        var filePath = "/uploads/invoice.pdf";

        _fileStorageMock
            .Setup(x => x.SaveFileAsync(It.IsAny<Stream>(), fileName, contentType, It.IsAny<CancellationToken>()))
            .ReturnsAsync(filePath);

        _openAIClientMock
            .Setup(x => x.ExtractStructuredDataFromPdfAsync<InvoiceExtractionResultDto>(
                filePath,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InvoiceExtractionResultDto
            {
                InvoiceNumber = string.Empty,
                SupplierName = string.Empty,
                TotalAmount = 0,
                SubtotalAmount = 0,
                Currency = "EUR"
            });

        var command = new UploadInvoiceCommand(fileStream, fileName, contentType);

        // Act & Assert
        var act = async () => await _useCase.ExecuteAsync(command);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("El número de factura no puede estar vacío.");
    }

}
