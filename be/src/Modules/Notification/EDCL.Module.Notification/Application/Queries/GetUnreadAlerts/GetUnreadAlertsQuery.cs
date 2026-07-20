using EDCL.Module.Notification.Application.Queries.GetNotificationLogs;
using EDCL.Shared.Kernel.Common;
using MediatR;

namespace EDCL.Module.Notification.Application.Queries.GetUnreadAlerts;

public sealed record GetUnreadAlertsQuery() : IRequest<Result<IReadOnlyList<NotificationLogDto>>>;
