using EDCL.Module.Cargo.Api;
using EDCL.Shared.Kernel.Events;
using FluentAssertions;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace EDCL.UnitTests.Modules.Cargo.Controllers;

public class DeliveryControllerTests
{
    private readonly Mock<IPublishEndpoint> _publishEndpointMock;
    private readonly DeliveryController _controller;

    public DeliveryControllerTests()
    {
        _publishEndpointMock = new Mock<IPublishEndpoint>();
        _controller = new DeliveryController(_publishEndpointMock.Object);
    }

    [Fact]
    public async Task SimulateDelivery_ShouldReturnAccepted_WhenSuccess()
    {
        // Arrange
        var request = new DeliverySimulateRequest
        {
            ManifestId = 1,
            ManifestNo = "MNF-123",
            Status = "Delivered",
            Remarks = "Test"
        };

        // Act
        var result = await _controller.SimulateDelivery(request);

        // Assert
        var acceptedResult = result.Should().BeOfType<AcceptedResult>().Subject;
        
        _publishEndpointMock.Verify(x => x.Publish(It.Is<ManifestDeliveredIntegrationEvent>(e => 
            e.ManifestId == request.ManifestId &&
            e.ManifestNo == request.ManifestNo &&
            e.Status == request.Status
        ), It.IsAny<CancellationToken>()), Times.Once);
    }
}
