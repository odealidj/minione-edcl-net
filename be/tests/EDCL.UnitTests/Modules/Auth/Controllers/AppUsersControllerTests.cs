using EDCL.Module.Auth.Application.Commands.RegisterAppUser;
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

public class AppUsersControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly AppUsersController _controller;

    public AppUsersControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _controller = new AppUsersController(_mediatorMock.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "test-trace-id";
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task RegisterAppUser_ShouldReturnCreated_WhenSuccess()
    {
        // Arrange
        var command = new RegisterAppUserCommand("Admin", "admin@edcl.com", "Password123!", "ADMIN");
        var response = new RegisterAppUserResponse(1, "admin@edcl.com", "Admin", "ADMIN");
        
        _mediatorMock.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RegisterAppUserResponse>.Success(response));

        // Act
        var result = await _controller.RegisterAppUser(command, CancellationToken.None);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedResult>().Subject;
        var apiResponse = createdResult.Value.Should().BeOfType<ApiResponse<RegisterAppUserResponse>>().Subject;
        apiResponse.Data.Should().Be(response);
    }

    [Fact]
    public async Task RegisterAppUser_ShouldReturnConflict_WhenEmailInUse()
    {
        // Arrange
        var command = new RegisterAppUserCommand("Admin", "admin@edcl.com", "Password123!", "ADMIN");
        
        _mediatorMock.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RegisterAppUserResponse>.Failure(new Error("AppUser.EmailInUse", "Email is in use.", ErrorType.Conflict)));

        // Act
        var result = await _controller.RegisterAppUser(command, CancellationToken.None);

        // Assert
        var conflictResult = result.Should().BeOfType<ConflictObjectResult>().Subject;
        var apiResponse = conflictResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        apiResponse.Message.Should().Be("Email is in use.");
    }

    [Fact]
    public async Task LoginAppUser_ShouldReturnOk_WhenSuccess()
    {
        // Arrange
        var command = new EDCL.Module.Auth.Application.Commands.Login.LoginAppUserCommand("admin@edcl.com", "Password123!");
        var response = new EDCL.Module.Auth.Application.Commands.Login.LoginAppUserResponse("access", DateTime.UtcNow, "refresh");
        
        _mediatorMock.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<EDCL.Module.Auth.Application.Commands.Login.LoginAppUserResponse>.Success(response));

        // Act
        var result = await _controller.LoginAppUser(command, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<EDCL.Module.Auth.Application.Commands.Login.LoginAppUserResponse>>().Subject;
        apiResponse.Data.Should().Be(response);
    }
}
