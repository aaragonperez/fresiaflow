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
    private readonly Mock<IInvoiceRepository> _invoiceRepositoryMock;
    private readonly Mock<IFileStorage> _fileStorageMock;
    private readonly Mock<IOpenAIClient> _openAIClientMock;
    private readonly Mock<IPdfTextExtractorService> _pdfTextExtractorMock;
    private readonly InvoiceExtractionPromptOptions _promptOptions;
    private readonly UploadInvoiceUseCase _useCase;

    public UploadInvoiceUseCaseTests()
    {
        _invoiceRepositoryMock = new Mock<IInvoiceRepository>();
        _fileStorageMock = new Mock<IFileStorage>();
        _openAIClientMock = new Mock<IOpenAIClient>();
        _pdfTextExtractorMock = new Mock<IPdfTextExtractorService>();
        
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
            _pdfTextExtractorMock.Object,
            optionsMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyPdfText_ShouldThrowException()
    {
        // Arrange
        var fileStream = new MemoryStream();
        var fileName = "invoice.pdf";
        var contentType = "application/pdf";
        var filePath = "/uploads/invoice.pdf";

        _fileStorageMock
            .Setup(x => x.SaveFileAsync(It.IsAny<Stream>(), fileName, contentType, It.IsAny<CancellationToken>()))
            .ReturnsAsync(filePath);

        _pdfTextExtractorMock
            .Setup(x => x.ExtractTextAsync(filePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);

        var command = new UploadInvoiceCommand(fileStream, fileName, contentType);

        // Act & Assert
        var act = async () => await _useCase.ExecuteAsync(command);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("No se pudo extraer texto del PDF.*");
    }

}
