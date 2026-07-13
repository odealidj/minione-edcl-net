using EDCL.Module.Job.Application.Commands.CompleteStop;
using EDCL.Module.Job.Application.Ports;
using EDCL.Module.Job.Domain.Entities;
using FluentAssertions;
using Moq;
using Xunit;

using EDCL.Shared.Kernel.Ports;

namespace EDCL.UnitTests.Modules.Job;

public class CompleteStopCommandHandlerTests
{
    private readonly Mock<IPickupOrderRepository> _repositoryMock;
    private readonly Mock<ISupplierPort> _supplierPortMock;
    private readonly CompleteStopCommandHandler _handler;

    public CompleteStopCommandHandlerTests()
    {
        _repositoryMock = new Mock<IPickupOrderRepository>();
        _supplierPortMock = new Mock<ISupplierPort>();
        _handler = new CompleteStopCommandHandler(_repositoryMock.Object, _supplierPortMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenStopNotFound()
    {
        // Arrange
        _repositoryMock.Setup(x => x.GetStopByIdWithKanbansAsync(It.IsAny<long>(), default))
            .ReturnsAsync((PickupOrderDetail?)null);

        var command = new CompleteStopCommand(StopId: 1, DriverId: 100, Latitude: -6.2, Longitude: 106.8);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Stop.NotFound");
    }
}
