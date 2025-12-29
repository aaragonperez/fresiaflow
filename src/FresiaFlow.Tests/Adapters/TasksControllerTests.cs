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
    private readonly Mock<IProposeDailyPlanUseCase> _proposeDailyPlanUseCaseMock = new();
    private readonly Mock<ICreateTaskUseCase> _createTaskUseCaseMock = new();
    private readonly Mock<ITaskManagementUseCase> _taskManagementUseCaseMock = new();
    private readonly TasksController _controller;

    public TasksControllerTests()
    {
        _controller = new TasksController(
            _proposeDailyPlanUseCaseMock.Object,
            _createTaskUseCaseMock.Object,
            _taskManagementUseCaseMock.Object);
    }

    [Fact]
    public async Task GetPendingTasks_ShouldReturnOk()
    {
        var expected = new List<TaskItem> { new TaskItem("t1", "d", TaskPriority.Low) };
        _taskManagementUseCaseMock.Setup(x => x.GetPendingTasksAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _controller.GetPendingTasks(null, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        result.As<OkObjectResult>().Value.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task GetTaskById_ShouldReturnNotFound()
    {
        var id = Guid.NewGuid();
        _taskManagementUseCaseMock.Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaskItem?)null);

        var result = await _controller.GetTaskById(id, CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetTaskById_ShouldReturnOk()
    {
        var id = Guid.NewGuid();
        var task = new TaskItem("t1", "d", TaskPriority.Low);
        _taskManagementUseCaseMock.Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        var result = await _controller.GetTaskById(id, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        result.As<OkObjectResult>().Value.Should().Be(task);
    }

    [Fact]
    public async Task CreateTask_WithValidDto_ShouldReturnCreated()
    {
        var dto = new CreateTaskDto
        {
            Title = "Test Task",
            Description = "Test Description"
        };

        var task = new TaskItem("Test Task", "Test Description", TaskPriority.Medium);
        var useCaseResult = new CreateTaskResult(task);
        _createTaskUseCaseMock.Setup(x => x.ExecuteAsync(It.IsAny<CreateTaskCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(useCaseResult);

        var result = await _controller.CreateTask(dto, CancellationToken.None);

        result.Should().BeOfType<CreatedAtActionResult>();
        var created = result as CreatedAtActionResult;
        created!.Value.Should().Be(task);
        created.RouteValues!["id"].Should().Be(task.Id);
    }

    [Fact]
    public async Task CompleteTask_ShouldReturnNoContent()
    {
        var id = Guid.NewGuid();

        var result = await _controller.CompleteTask(id, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        _taskManagementUseCaseMock.Verify(x => x.CompleteAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CompleteTask_NotFound_ShouldReturnNotFound()
    {
        _taskManagementUseCaseMock.Setup(x => x.CompleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException());

        var result = await _controller.CompleteTask(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task UpdateTaskPriority_ShouldReturnNoContent()
    {
        var id = Guid.NewGuid();
        var dto = new UpdatePriorityDto { Priority = 1 };

        var result = await _controller.UpdateTaskPriority(id, dto, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        _taskManagementUseCaseMock.Verify(x => x.UpdatePriorityAsync(id, TaskPriority.Medium, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateTaskPriority_NotFound_ShouldReturnNotFound()
    {
        _taskManagementUseCaseMock.Setup(x => x.UpdatePriorityAsync(It.IsAny<Guid>(), It.IsAny<TaskPriority>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException());

        var result = await _controller.UpdateTaskPriority(Guid.NewGuid(), new UpdatePriorityDto { Priority = 1 }, CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DeleteTask_ShouldReturnNoContent()
    {
        var id = Guid.NewGuid();

        var result = await _controller.DeleteTask(id, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        _taskManagementUseCaseMock.Verify(x => x.DeleteAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProposeDailyPlan_WithValidDto_ShouldReturnOk()
    {
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

        var result = await _controller.ProposeDailyPlan(dto, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().Be(expectedResult);
    }
}

