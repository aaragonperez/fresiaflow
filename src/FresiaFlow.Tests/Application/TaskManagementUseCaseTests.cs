using FresiaFlow.Application.Ports.Outbound;
using FresiaFlow.Application.UseCases;
using FresiaFlow.Domain.Tasks;
using FluentAssertions;
using Moq;

namespace FresiaFlow.Tests.Application;

public class TaskManagementUseCaseTests
{
    private readonly Mock<ITaskRepository> _repo = new();
    private readonly TaskManagementUseCase _useCase;

    public TaskManagementUseCaseTests()
    {
        _useCase = new TaskManagementUseCase(_repo.Object);
    }

    [Fact]
    public async Task CompleteAsync_WhenTaskNotFound_ShouldThrowInvalidOperation()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaskItem?)null);

        await FluentActions.Invoking(() => _useCase.CompleteAsync(Guid.NewGuid(), CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CompleteAsync_ShouldMarkAndUpdate()
    {
        var task = new TaskItem("t", "d", TaskPriority.Low);
        _repo.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);

        await _useCase.CompleteAsync(task.Id, CancellationToken.None);

        task.IsCompleted.Should().BeTrue();
        _repo.Verify(r => r.UpdateAsync(task, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdatePriorityAsync_ShouldChangePriority()
    {
        var task = new TaskItem("t", "d", TaskPriority.Low);
        _repo.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);

        await _useCase.UpdatePriorityAsync(task.Id, TaskPriority.High, CancellationToken.None);

        task.Priority.Should().Be(TaskPriority.High);
        _repo.Verify(r => r.UpdateAsync(task, It.IsAny<CancellationToken>()), Times.Once);
    }
}

