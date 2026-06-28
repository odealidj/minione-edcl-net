using EDCL.Module.Cargo.Api;
using EDCL.Module.Cargo.Application.Queries.GetManifestParts;
using EDCL.Module.Cargo.Application.Queries.GetManifests;
using EDCL.Shared.Http.Responses;
using EDCL.Shared.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Common;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace EDCL.UnitTests.Modules.Cargo.Controllers;

public class CargoControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly CargoController _controller;

    public CargoControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        
        _currentUserServiceMock.Setup(x => x.CurrentTraceId).Returns("test-trace-id");

        _controller = new CargoController(_mediatorMock.Object, _currentUserServiceMock.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "test-trace-id";
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task GetManifests_ShouldReturnOk_WhenSuccess()
    {
        // Arrange
        var response = new ManifestsResponse(1, 10, 5, new List<ManifestDto>());
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetManifestsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ManifestsResponse>.Success(response));

        // Act
        var result = await _controller.GetManifests(1, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<ManifestsResponse>>().Subject;
        apiResponse.Data.Should().Be(response);
    }

    [Fact]
    public async Task GetManifestParts_ShouldReturnOk_WhenSuccess()
    {
        // Arrange
        var response = new ManifestPartsResponse(1, "MNF-123", 10, 5, new List<ManifestPartDto>());
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetManifestPartsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ManifestPartsResponse>.Success(response));

        // Act
        var result = await _controller.GetManifestParts(1, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<ManifestPartsResponse>>().Subject;
        apiResponse.Data.Should().Be(response);
    }

    [Fact]
    public async Task GetManifestParts_ShouldReturnNotFound_WhenFail()
    {
        // Arrange
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetManifestPartsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ManifestPartsResponse>.Failure(new Error("Cargo.NotFound", "Not found", ErrorType.NotFound)));

        // Act
        var result = await _controller.GetManifestParts(1, CancellationToken.None);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var apiResponse = notFoundResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        apiResponse.Message.Should().Be("Not found");
    }
}
