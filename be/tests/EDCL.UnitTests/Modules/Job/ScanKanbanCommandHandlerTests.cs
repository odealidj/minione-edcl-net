using EDCL.Module.Job.Application.Commands.ScanKanban;
using EDCL.Module.Job.Application.Ports;
using EDCL.Module.Job.Domain.Entities;
using FluentAssertions;
using Moq;
using Xunit;

namespace EDCL.UnitTests.Modules.Job;

public class ScanKanbanCommandHandlerTests
{
    private readonly Mock<IPickupOrderRepository> _repositoryMock;
    private readonly ScanKanbanCommandHandler _handler;

    public ScanKanbanCommandHandlerTests()
    {
        _repositoryMock = new Mock<IPickupOrderRepository>();
        _handler = new ScanKanbanCommandHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenStopNotFound()
    {
        // Arrange
        _repositoryMock.Setup(x => x.GetStopByIdWithKanbansAsync(It.IsAny<long>(), default))
            .ReturnsAsync((PickupOrderDetail?)null);

        var command = new ScanKanbanCommand(StopId: 1, ManifestId: 1, KanbanCode: "KB01", DriverId: 100);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Stop.NotFound");
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenDriverUnauthorized()
    {
        // Arrange
        var stop = PickupOrderDetail.Create(1, 1, 1);
        
        _repositoryMock.Setup(x => x.GetStopByIdWithKanbansAsync(It.IsAny<long>(), default))
            .ReturnsAsync(stop); // PickupOrder is null

        var command = new ScanKanbanCommand(StopId: 1, ManifestId: 1, KanbanCode: "KB01", DriverId: 100);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Stop.Unauthorized");
    }
}
