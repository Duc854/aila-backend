using AILA.Application.Common.Interfaces;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Notifications.Commands
{
    public class MarkNotificationReadCommandHandler
        : IRequestHandler<MarkNotificationReadCommand, ResponseDto<object>>
    {
        private readonly IUnitOfWork _uow;
        public MarkNotificationReadCommandHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ResponseDto<object>> Handle(
            MarkNotificationReadCommand request,
            CancellationToken ct)
        {
            await _uow.Notifications.MarkAsReadAsync(
                request.NotificationId, request.UserId, ct);
            await _uow.SaveChangesAsync(ct);
            return ResponseDto<object>.SuccessResult(null!);
        }
    }

    public class MarkAllNotificationsReadCommandHandler
        : IRequestHandler<MarkAllNotificationsReadCommand, ResponseDto<object>>
    {
        private readonly IUnitOfWork _uow;
        public MarkAllNotificationsReadCommandHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ResponseDto<object>> Handle(
            MarkAllNotificationsReadCommand request,
            CancellationToken ct)
        {
            await _uow.Notifications.MarkAllAsReadAsync(request.UserId, ct);
            await _uow.SaveChangesAsync(ct);
            return ResponseDto<object>.SuccessResult(null!);
        }
    }
}