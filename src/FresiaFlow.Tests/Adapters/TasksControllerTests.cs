using FresiaFlow.Adapters.Inbound.Api.Controllers;
using FresiaFlow.Adapters.Inbound.Api.Dtos;
using FresiaFlow.Application.Ports.Inbound;
using FresiaFlow.Domain.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FresiaFlow.Tests.Adapters;

public class TasksControllerTests
{
    private readonly Mock<IProposeDailyPlanUseCase> _proposeDailyPlanUseCaseMock;
    private readonly TasksController _controller;

    public TasksControllerTests()
    {
        _proposeDailyPlanUseCaseMock = new Mock<IProposeDailyPlanUseCase>();
        _controller = new TasksController(_proposeDailyPlanUseCaseMock.Object);
    }

    [Fact]
    public async Task GetPendingTasks_ShouldReturnOk()
    {
        // Act
        var result = await _controller.GetPendingTasks(null, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetTaskById_ShouldReturnNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var result = await _controller.GetTaskById(id, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task CreateTask_WithValidDto_ShouldReturnOk()
    {
        // Arrange
        var dto = new CreateTaskDto
        {
            Title = "Test Task",
            Description = "Test Description"
        };

        // Act
        var result = await _controller.CreateTask(dto, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task CompleteTask_ShouldReturnNoContent()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var result = await _controller.CompleteTask(id, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task UpdateTaskPriority_ShouldReturnNoContent()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new UpdatePriorityDto { Priority = 1 };

        // Act
        var result = await _controller.UpdateTaskPriority(id, dto, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteTask_ShouldReturnNoContent()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var result = await _controller.DeleteTask(id, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task ProposeDailyPlan_WithValidDto_ShouldReturnOk()
    {
        // Arrange
        var dto = new ProposeDailyPlanDto
        {
            Date = DateTime.Today,
            IncludePendingInvoices = true,
            IncludeUnreconciledTransactions = false
        };

        var expectedResult = new DailyPlanResult(
            new List<TaskItem>(),
            "Test summary",
            new List<string>());

        _proposeDailyPlanUseCaseMock
            .Setup(x => x.ExecuteAsync(
                It.IsAny<ProposeDailyPlanCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.ProposeDailyPlan(dto, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().Be(expectedResult);
    }
}

