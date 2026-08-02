using AILA.Application.Features.SubscriptionPlans.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.SubscriptionPlans.Commands.ChangeSubscriptionPlanStatus
{
    /// <summary>
    /// UC-92 - Manage Subscription Plan Status (Allowed Roles: Admin).
    /// IsActive = true → Activate, false → Deactivate.
    /// Bước xác nhận (BR-04) enforce ở UI; lệnh này chỉ được gửi sau khi admin đã xác nhận.
    /// </summary>
    public record ChangeSubscriptionPlanStatusCommand(
        Guid PlanId,
        bool IsActive
    ) : IRequest<ResponseDto<AdminSubscriptionPlanDto>>;
}
