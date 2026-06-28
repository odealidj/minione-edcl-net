using EDCL.Module.Auth.Application.Commands.CreateDriver;
using EDCL.Module.Auth.Controllers;
using EDCL.Shared.Http.Responses;
using EDCL.Shared.Kernel.Common;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace EDCL.UnitTests.Modules.Auth.Controllers;

public class AdminDriversControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly AdminDriversController _controller;

    public AdminDriversControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _controller = new AdminDriversController(_mediatorMock.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "test-trace-id";
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task CreateDriver_ShouldReturnCreated_WhenSuccess()
    {
        // Arrange
        var command = new CreateDriverCommand("08123456789", "John Doe", "1234567890123456", 1);
        var response = new CreateDriverResponse(1, "John Doe", "1234567890123456", "08123456789", 1);
        
        _mediatorMock.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CreateDriverResponse>.Success(response));

        // Act
        var result = await _controller.CreateDriver(command, CancellationToken.None);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedResult>().Subject;
        var apiResponse = createdResult.Value.Should().BeOfType<ApiResponse<CreateDriverResponse>>().Subject;
        apiResponse.Data.Should().Be(response);
    }

    [Fact]
    public async Task CreateDriver_ShouldReturnConflict_WhenPhoneInUse()
    {
        // Arrange
        var command = new CreateDriverCommand("08123456789", "John Doe", "1234567890123456", 1);
        
        _mediatorMock.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CreateDriverResponse>.Failure(new Error("Driver.PhoneInUse", "Phone in use.", ErrorType.Conflict)));

        // Act
        var result = await _controller.CreateDriver(command, CancellationToken.None);

        // Assert
        var conflictResult = result.Should().BeOfType<ConflictObjectResult>().Subject;
        var apiResponse = conflictResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        apiResponse.Message.Should().Be("Phone in use.");
    }
}
