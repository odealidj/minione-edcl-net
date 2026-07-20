using EDCL.Module.Notification.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EDCL.Module.Notification.Application.Commands.AcknowledgeAlert;

public sealed class AcknowledgeAlertCommandHandler(NotificationDbContext dbContext)
    : IRequestHandler<AcknowledgeAlertCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(AcknowledgeAlertCommand request, CancellationToken cancellationToken)
    {
        var notification = await dbContext.DriverNotifications
            .FirstOrDefaultAsync(n => n.Id == request.NotificationId, cancellationToken);

        if (notification == null)
            return Error.NotFound("Notification", request.NotificationId);

        notification.AcknowledgeAdminAlert();
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
