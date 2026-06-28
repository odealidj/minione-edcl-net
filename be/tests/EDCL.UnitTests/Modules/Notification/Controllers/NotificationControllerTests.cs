using EDCL.Module.Notification.Api;
using EDCL.Module.Notification.Application.Commands.MarkNotificationRead;
using EDCL.Module.Notification.Application.Queries.GetNotifications;
using EDCL.Shared.Http.Responses;
using EDCL.Shared.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Common;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace EDCL.UnitTests.Modules.Notification.Controllers;

public class NotificationControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly NotificationController _controller;

    public NotificationControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        
        _currentUserServiceMock.Setup(x => x.DriverId).Returns(1);
        _currentUserServiceMock.Setup(x => x.CurrentTraceId).Returns("test-trace-id");

        _controller = new NotificationController(_mediatorMock.Object, _currentUserServiceMock.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "test-trace-id";
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task GetNotifications_ShouldReturnOk_WhenSuccess()
    {
        // Arrange
        var response = new NotificationsResponse(1, 0, new List<NotificationDto>());
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetNotificationsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<NotificationsResponse>.Success(response));

        // Act
        var result = await _controller.GetNotifications(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<NotificationsResponse>>().Subject;
        apiResponse.Data.Should().Be(response);
    }

    [Fact]
    public async Task MarkAsRead_ShouldReturnOk_WhenSuccess()
    {
        // Arrange
        _mediatorMock.Setup(m => m.Send(It.IsAny<MarkNotificationReadCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        // Act
        var result = await _controller.MarkAsRead(1, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<bool>>().Subject;
        apiResponse.Data.Should().BeTrue();
    }

    [Fact]
    public async Task GetNotifications_ShouldReturnUnauthorized_WhenDriverIdNull()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.DriverId).Returns((long?)null);

        // Act
        var result = await _controller.GetNotifications(CancellationToken.None);

        // Assert
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var apiResponse = unauthorizedResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        apiResponse.Message.Should().Be("DriverId is required");
    }
}
