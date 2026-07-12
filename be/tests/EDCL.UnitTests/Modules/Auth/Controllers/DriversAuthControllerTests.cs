using EDCL.Module.Auth.Application.Commands.ChangeDriverPin;
using EDCL.Module.Auth.Application.Commands.Login;
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

public class DriversAuthControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly DriversAuthController _controller;

    public DriversAuthControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _controller = new DriversAuthController(_mediatorMock.Object);

        // Mock HttpContext for GetTraceId()
        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "test-trace-id";
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task Login_ShouldReturnOk_WhenSuccess()
    {
        // Arrange
        var command = new LoginCommand("08123456789", "123456", "TestDevice");
        var loginResponse = new LoginResponse(1, "John Doe", "1234567890123456", "avatar.jpg", "LogisticPartner A", "access_token", "refresh_token", DateTime.UtcNow.AddMinutes(15), DateTime.UtcNow.AddDays(30));
        
        _mediatorMock.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<LoginResponse>.Success(loginResponse));

        // Act
        var result = await _controller.Login(command, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<LoginResponse>>().Subject;
        apiResponse.Data.Should().Be(loginResponse);
        apiResponse.Code.Should().Be(200);
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenForceChangePin()
    {
        // Arrange
        var command = new LoginCommand("08123456789", "123456", "TestDevice");
        
        _mediatorMock.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<LoginResponse>.Failure(new Error("Auth.ForceChangePin", "You must change your PIN.", ErrorType.Unauthorized)));

        // Act
        var result = await _controller.Login(command, CancellationToken.None);

        // Assert
        var unauthorizedResult = result.Should().BeOfType<ObjectResult>().Subject;
        unauthorizedResult.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        
        var apiResponse = unauthorizedResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        apiResponse.Message.Should().Be("You must change your PIN.");
        apiResponse.Errors.Should().ContainSingle(e => e.Code == "Auth.ForceChangePin");
    }

    [Fact]
    public async Task ChangePin_ShouldReturnOk_WhenSuccess()
    {
        // Arrange
        var command = new ChangeDriverPinCommand("08123456789", "123456", "654321", "TestDevice");
        var loginResponse = new LoginResponse(1, "John Doe", "1234567890123456", "avatar.jpg", "LogisticPartner A", "access_token", "refresh_token", DateTime.UtcNow.AddMinutes(15), DateTime.UtcNow.AddDays(30));
        
        _mediatorMock.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<LoginResponse>.Success(loginResponse));

        // Act
        var result = await _controller.ChangePin(command, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<LoginResponse>>().Subject;
        apiResponse.Data.Should().Be(loginResponse);
    }

    [Fact]
    public async Task ChangePin_ShouldReturnBadRequest_WhenValidationFails()
    {
        // Arrange
        var command = new ChangeDriverPinCommand("08123456789", "123456", "654321", "TestDevice");
        
        _mediatorMock.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<LoginResponse>.Failure(new Error("Auth.InvalidNewPin", "PIN is invalid.", ErrorType.Validation)));

        // Act
        var result = await _controller.ChangePin(command, CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var apiResponse = badRequestResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        apiResponse.Message.Should().Be("PIN is invalid.");
    }
}
