using EDCL.Module.Cargo.Api;
using EDCL.Module.Cargo.Application.Queries.GetManifestDetail;
using EDCL.Shared.Http.Responses;
using EDCL.Shared.Kernel.Common;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace EDCL.UnitTests.Modules.Cargo.Controllers;

public class ManifestControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly ManifestController _controller;

    public ManifestControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _controller = new ManifestController(_mediatorMock.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "test-trace-id";
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task GetManifestDetail_ShouldReturnOk_WhenSuccess()
    {
        // Arrange
        var response = new ManifestDetailDto("Cycle-1", "DEL-1", "MNF-123", 10, "ORD-1", "DOCK-1", "LANE-1", new List<ManifestPartDto>());
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetManifestDetailQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ManifestDetailDto>.Success(response));

        // Act
        var result = await _controller.GetManifestDetail("MNF-123", CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<ManifestDetailDto>>().Subject;
        apiResponse.Data.Should().Be(response);
    }
}
