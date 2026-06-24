namespace EDCL.Module.Notification.Application.Commands.MarkNotificationRead;

using EDCL.Module.Notification.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

public sealed class MarkNotificationReadCommandHandler(NotificationDbContext dbContext)
    : IRequestHandler<MarkNotificationReadCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await dbContext.DriverNotifications
            .FirstOrDefaultAsync(n => n.Id == request.NotificationId && n.DriverId == request.DriverId, cancellationToken);

        if (notification == null)
        {
            return Error.NotFound("Notification", request.NotificationId);
        }

        notification.MarkAsRead();
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
