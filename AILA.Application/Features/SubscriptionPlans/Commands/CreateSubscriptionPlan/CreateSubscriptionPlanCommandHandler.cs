using AILA.Application.Common.Exceptions;
using AILA.Application.Common.Interfaces;
using AILA.Application.Features.SubscriptionPlans.Dtos;
using AILA.Application.Features.SubscriptionPlans.Mapping;
using AILA.Domain.Entities;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.SubscriptionPlans.Commands.CreateSubscriptionPlan
{
    /// <summary>
    /// UC-90 - Create Subscription Plan.
    /// </summary>
    public class CreateSubscriptionPlanCommandHandler(IUnitOfWork uow)
        : IRequestHandler<CreateSubscriptionPlanCommand, ResponseDto<AdminSubscriptionPlanDto>>
    {
        public async Task<ResponseDto<AdminSubscriptionPlanDto>> Handle(
            CreateSubscriptionPlanCommand request,
            CancellationToken ct)
        {
            // AC-90.2 → AC-90.9: kiểm tra từng field để trả mã lỗi bám đúng field vi phạm.
            var createOnlyError = SubscriptionPlanRules.ValidateCreateOnlyFields(
                request.Name,
                request.TierLevel,
                request.DurationInDays);

            if (createOnlyError is not null)
                return ResponseDto<AdminSubscriptionPlanDto>.FailResult(
                    createOnlyError.Value.Code,
                    createOnlyError.Value.Message);

            var commonError = SubscriptionPlanRules.ValidateCommonFields(
                request.Description,
                request.Price,
                request.AiTokenLimit,
                request.AiPracticeScenarioLimit,
                request.ExpertEvaluationLimit,
                request.DisplayOrder);

            if (commonError is not null)
                return ResponseDto<AdminSubscriptionPlanDto>.FailResult(
                    commonError.Value.Code,
                    commonError.Value.Message);

            // AC-90.3: entity Trim() khi gán, so trùng cũng phải dựa trên giá trị đã Trim.
            var name = request.Name.Trim();

            // AC-90.10 / BR-01 / INV-02: tính duy nhất của Name không enforce được trong entity.
            if (await uow.SubscriptionPlans.ExistsByNameAsync(name, ct))
                return ResponseDto<AdminSubscriptionPlanDto>.FailResult(
                    SubscriptionPlanErrors.NameAlreadyExists,
                    "Tên gói đăng ký đã tồn tại.");

            // AC-90.11 / BR-02 / INV-02
            if (await uow.SubscriptionPlans.ExistsByTierLevelAsync(request.TierLevel, ct))
                return ResponseDto<AdminSubscriptionPlanDto>.FailResult(
                    SubscriptionPlanErrors.TierLevelAlreadyExists,
                    "Cấp độ gói đã tồn tại.");

            SubscriptionPlan plan;

            try
            {
                // AC-90.1 / BR-05: Status = Active do constructor gán, không nhận từ input.
                plan = new SubscriptionPlan(
                    name,
                    request.Description,
                    request.Price,
                    request.TierLevel,
                    request.DurationInDays,
                    request.AiTokenLimit,
                    request.AiPracticeScenarioLimit,
                    request.ExpertEvaluationLimit,
                    request.DisplayOrder);
            }
            catch (ArgumentException ex)
            {
                // Hàng rào cuối của domain: nếu rule ở trên lệch so với entity thì vẫn trả
                // lỗi validation thay vì 500 (AC-90.13).
                return ResponseDto<AdminSubscriptionPlanDto>.FailResult(
                    SubscriptionPlanErrors.ValidationError,
                    ex.Message);
            }

            await uow.SubscriptionPlans.AddAsync(plan);

            try
            {
                await uow.SaveChangesAsync(ct);
            }
            catch (DuplicateKeyException ex)
            {
                // Edge case: hai admin tạo trùng gần như đồng thời — unique index ở DB chặn,
                // dịch thành lỗi validation thay vì để lộ lỗi hạ tầng.
                var (code, message) = SubscriptionPlanRules.MapDuplicateConstraint(ex.ConstraintName);

                return ResponseDto<AdminSubscriptionPlanDto>.FailResult(code, message);
            }

            return ResponseDto<AdminSubscriptionPlanDto>.SuccessResult(plan.ToAdminDto());
        }
    }
}
