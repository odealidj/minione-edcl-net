using EDCL.Shared.Kernel.Common;
using MediatR;

namespace EDCL.Module.Notification.Application.Commands.ResendNotification;

public sealed record ResendNotificationCommand(long NotificationId) : IRequest<Result<bool>>;
