using EDCL.Shared.Kernel.Common;
using MediatR;

namespace EDCL.Module.Notification.Application.Commands.AcknowledgeAlert;

public sealed record AcknowledgeAlertCommand(long NotificationId) : IRequest<Result<bool>>;
