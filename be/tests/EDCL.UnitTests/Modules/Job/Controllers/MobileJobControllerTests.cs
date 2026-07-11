using EDCL.Module.Job.Api;
using EDCL.Module.Job.Application.Commands.AssignJob;
using EDCL.Module.Job.Application.Commands.CompleteStop;
using EDCL.Module.Job.Application.Commands.EndJob;
using EDCL.Module.Job.Application.Commands.ScanKanban;
using EDCL.Module.Job.Application.Commands.StartJob;
using EDCL.Module.Job.Application.Queries.GetDashboard;
using EDCL.Module.Job.Application.Queries.GetManifests;
using EDCL.Module.Job.Application.Queries.GetRouteStops;
using EDCL.Shared.Http.Responses;
using EDCL.Shared.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Common;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace EDCL.UnitTests.Modules.Job.Controllers;

public class MobileJobControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly MobileJobController _controller;

    public MobileJobControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        
        _currentUserServiceMock.Setup(x => x.DriverId).Returns(1);
        _currentUserServiceMock.Setup(x => x.CurrentTraceId).Returns("test-trace-id");

        _controller = new MobileJobController(_mediatorMock.Object, _currentUserServiceMock.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "test-trace-id";
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task GetDashboard_ShouldReturnOk_WhenSuccess()
    {
        // Arrange
        var response = new DashboardResponse(new DriverProfileDto("John", null, null), null, null);
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetDashboardQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<DashboardResponse>.Success(response));

        // Act
        var result = await _controller.GetDashboard(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<DashboardResponse>>().Subject;
        apiResponse.Data.Should().Be(response);
    }

    [Fact]
    public async Task StartJob_ShouldReturnOk_WhenSuccess()
    {
        // Arrange
        _mediatorMock.Setup(m => m.Send(It.IsAny<StartJobCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        // Act
        var result = await _controller.StartJob(1, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<bool>>().Subject;
        apiResponse.Data.Should().BeTrue();
    }

    // Removed AssignJob test because it was moved to AdminPickupOrdersController

    [Fact]
    public async Task GetRouteStops_ShouldReturnOk_WhenSuccess()
    {
        // Arrange
        var response = new RouteStopsResponse("ROUTE-1", "CYCLE-1", "DEL-1", "2023-10-01", new List<RouteStopDto>());
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetRouteStopsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RouteStopsResponse>.Success(response));

        // Act
        var result = await _controller.GetRouteStops(1, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<RouteStopsResponse>>().Subject;
        apiResponse.Data.Should().Be(response);
    }

    [Fact]
    public async Task CompleteStop_ShouldReturnOk_WhenSuccess()
    {
        // Arrange
        _mediatorMock.Setup(m => m.Send(It.IsAny<CompleteStopCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        // Act
        var result = await _controller.CompleteStop(1, new CompleteStopRequest(-6.2, 106.8), CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<bool>>().Subject;
        apiResponse.Data.Should().BeTrue();
    }

    [Fact]
    public async Task GetManifests_ShouldReturnOk_WhenSuccess()
    {
        // Arrange
        var response = new ManifestListResponse("Supplier A", "A1", new List<ManifestDto>());
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetManifestsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ManifestListResponse>.Success(response));

        // Act
        var result = await _controller.GetManifests(1, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<ManifestListResponse>>().Subject;
        apiResponse.Data.Should().Be(response);
    }

    [Fact]
    public async Task ScanKanban_ShouldReturnOk_WhenSuccess()
    {
        // Arrange
        var response = new ScanKanbanResponse(100, "Scanned", 10, 10);
        _mediatorMock.Setup(m => m.Send(It.IsAny<ScanKanbanCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ScanKanbanResponse>.Success(response));

        // Act
        var result = await _controller.ScanKanban(1, 100, new ScanKanbanRequest("K-123"), CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<ScanKanbanResponse>>().Subject;
        apiResponse.Data.Should().Be(response);
    }

    [Fact]
    public async Task EndJob_ShouldReturnOk_WhenSuccess()
    {
        // Arrange
        _mediatorMock.Setup(m => m.Send(It.IsAny<EndJobCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        // Act
        var result = await _controller.EndJob(1, new EndJobRequest(-6.2, 106.8), CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<bool>>().Subject;
        apiResponse.Data.Should().BeTrue();
    }
}
