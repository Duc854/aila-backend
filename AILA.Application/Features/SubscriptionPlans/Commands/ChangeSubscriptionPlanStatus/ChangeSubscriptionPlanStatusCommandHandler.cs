using AILA.Application.Common.Interfaces;
using AILA.Application.Features.SubscriptionPlans.Dtos;
using AILA.Application.Features.SubscriptionPlans.Mapping;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.SubscriptionPlans.Commands.ChangeSubscriptionPlanStatus
{
    /// <summary>
    /// UC-92 - Manage Subscription Plan Status.
    /// Chỉ đổi trạng thái của plan; các subscription đã phát hành không bị đụng tới
    /// (AC-92.7, BR-02, INV-03).
    /// </summary>
    public class ChangeSubscriptionPlanStatusCommandHandler(IUnitOfWork uow)
        : IRequestHandler<ChangeSubscriptionPlanStatusCommand, ResponseDto<AdminSubscriptionPlanDto>>
    {
        public async Task<ResponseDto<AdminSubscriptionPlanDto>> Handle(
            ChangeSubscriptionPlanStatusCommand request,
            CancellationToken ct)
        {
            var plan = await uow.SubscriptionPlans.GetByIdAsync(request.PlanId);

            // AC-92.8
            if (plan is null)
                return ResponseDto<AdminSubscriptionPlanDto>.FailResult(
                    SubscriptionPlanErrors.NotFound,
                    "Không tìm thấy gói đăng ký.");

            // AC-92.3 / AC-92.4: entity từ chối khi plan đã ở trạng thái đích (idempotent-reject).
            // Bắt lại ở đây để trả thông báo thân thiện thay vì lỗi 500 — cũng là lớp chặn cho
            // double-click / gửi lặp.
            if (request.IsActive)
            {
                if (plan.IsActive())
                    return ResponseDto<AdminSubscriptionPlanDto>.FailResult(
                        SubscriptionPlanErrors.AlreadyActive,
                        "Gói đăng ký đã ở trạng thái hoạt động.");
            }
            else if (!plan.IsActive())
            {
                return ResponseDto<AdminSubscriptionPlanDto>.FailResult(
                    SubscriptionPlanErrors.AlreadyInactive,
                    "Gói đăng ký đã ở trạng thái ngừng hoạt động.");
            }

            try
            {
                // AC-92.1 / AC-92.2: đổi trạng thái qua đúng method của entity,
                // không set Status trực tiếp.
                if (request.IsActive)
                {
                    plan.Activate();
                }
                else
                {
                    plan.Deactivate();
                }
            }
            catch (ArgumentException ex)
            {
                return ResponseDto<AdminSubscriptionPlanDto>.FailResult(
                    SubscriptionPlanErrors.ValidationError,
                    ex.Message);
            }

            uow.SubscriptionPlans.Update(plan);

            await uow.SaveChangesAsync(ct);

            return ResponseDto<AdminSubscriptionPlanDto>.SuccessResult(plan.ToAdminDto());
        }
    }
}
