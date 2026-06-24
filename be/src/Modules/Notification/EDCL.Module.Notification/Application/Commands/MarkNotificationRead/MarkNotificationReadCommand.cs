namespace EDCL.Module.Notification.Application.Commands.MarkNotificationRead;

using EDCL.Shared.Kernel.Common;
using MediatR;

public sealed record MarkNotificationReadCommand(long NotificationId, long DriverId) : IRequest<Result<bool>>;
