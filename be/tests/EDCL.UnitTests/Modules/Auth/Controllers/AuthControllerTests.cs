using EDCL.Module.Auth.Api;
using EDCL.Module.Auth.Application.Commands.Login;
using EDCL.Module.Auth.Application.Commands.RefreshToken;
using EDCL.Module.Auth.Application.Commands.RequestOtp;
using EDCL.Module.Auth.Application.Commands.RevokeToken;
using EDCL.Shared.Http.Responses;
using EDCL.Shared.Kernel.Common;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace EDCL.UnitTests.Modules.Auth.Controllers;

public class AuthControllerTests
{
    private readonly Mock<ISender> _mediatorMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _mediatorMock = new Mock<ISender>();
        _controller = new AuthController(_mediatorMock.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "test-trace-id";
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task RequestOtp_ShouldReturnOk_WhenSuccess()
    {
        // Arrange
        var command = new RequestOtpCommand("08123456789");
        _mediatorMock.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RequestOtpResponse>.Success(new RequestOtpResponse(true, "OTP sent.")));

        // Act
        var result = await _controller.RequestOtp(command, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        apiResponse.Message.Should().Be("OTP sent.");
    }

    [Fact]
    public async Task Login_ShouldReturnOk_WhenSuccess()
    {
        // Arrange
        var request = new LoginRequest("08123456789", "123456");
        var loginResponse = new LoginResponse(1, "John", "1234567890123456", "img.jpg", "Transporter A", "access", "refresh", DateTime.UtcNow, DateTime.UtcNow);
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<LoginCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<LoginResponse>.Success(loginResponse));

        // Act
        // Set UserAgent header
        _controller.Request.Headers.UserAgent = "TestUserAgent";
        var result = await _controller.Login(request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<LoginResponse>>().Subject;
        apiResponse.Data.Should().Be(loginResponse);
    }

    [Fact]
    public async Task RefreshToken_ShouldReturnOk_WhenSuccess()
    {
        // Arrange
        var request = new RefreshTokenRequest("refresh_token_string");
        var response = new TokenPairResponse("access", "refresh", DateTime.UtcNow, DateTime.UtcNow);
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<RefreshTokenCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TokenPairResponse>.Success(response));

        // Act
        _controller.Request.Headers.UserAgent = "TestUserAgent";
        var result = await _controller.RefreshToken(request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<TokenPairResponse>>().Subject;
        apiResponse.Data.Should().Be(response);
    }

    [Fact]
    public async Task RevokeToken_ShouldReturnOk_WhenSuccess()
    {
        // Arrange
        var request = new RefreshTokenRequest("refresh_token_string");
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<RevokeTokenCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        // Act
        var result = await _controller.RevokeToken(request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        apiResponse.Message.Should().Be("Token revoked.");
    }
}
