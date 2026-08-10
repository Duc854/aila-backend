using MediatR;
using Shared.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.Notifications.Commands
{
    public record MarkNotificationReadCommand(Guid NotificationId, Guid UserId)
        : IRequest<ResponseDto<object>>;

    public record MarkAllNotificationsReadCommand(Guid UserId)
        : IRequest<ResponseDto<object>>;
}
