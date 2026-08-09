using AILA.Application.Common.Interfaces;
using AILA.Domain.Entities;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Payments.Commands.ConfirmPayment
{
    /// <summary>
    /// UC-19 Steps 4–7: Xử lý webhook SePay → xác nhận payment → áp dụng subscription policy.
    /// BR-03: Gia hạn (cùng tier) → Extend subscription hiện tại.
    /// BR-04: Nâng cấp (tier cao hơn) → Replace subscription cũ, tạo mới.
    /// AF-02: Payment không hoàn thành / hết hạn / bị huỷ → không thay đổi subscription.
    /// </summary>
    public class ConfirmPaymentCommandHandler(
        IUnitOfWork uow,
        ISePayService sePayService)
        : IRequestHandler<ConfirmPaymentCommand, ResponseDto<object>>
    {
        public async Task<ResponseDto<object>> Handle(
            ConfirmPaymentCommand request,
            CancellationToken cancellationToken)
        {
            // 1. Xác thực chữ ký webhook (bảo mật: chỉ chấp nhận từ SePay)
            if (!sePayService.VerifyWebhookSignature(request.RawBody, request.Signature))
                return ResponseDto<object>.FailResult(
                    PaymentErrors.InvalidSignature,
                    "Chữ ký webhook không hợp lệ.");

            var payload = request.Payload;

            // 2. Tìm payment theo orderCode
            var payment = await uow.Payments.GetByOrderCodeAsync(
                payload.OrderCode, cancellationToken);

            if (payment is null)
                return ResponseDto<object>.FailResult(
                    PaymentErrors.PaymentNotFound,
                    "Không tìm thấy giao dịch tương ứng.");

            // 3. Kiểm tra trạng thái payment (AF-02)
            if (!payment.IsPending())
                return ResponseDto<object>.FailResult(
                    PaymentErrors.AlreadyProcessed,
                    "Giao dịch đã được xử lý trước đó.");

            if (payment.IsExpired())
            {
                payment.MarkAsExpired();
                uow.Payments.Update(payment);
                await uow.SaveChangesAsync(cancellationToken);
                return ResponseDto<object>.FailResult(
                    PaymentErrors.PaymentExpired,
                    "Giao dịch đã hết hạn.");
            }

            // 4. Kiểm tra số tiền khớp (chống manipulation)
            if (payload.Amount != payment.Amount)
                return ResponseDto<object>.FailResult(
                    PaymentErrors.AmountMismatch,
                    "Số tiền thanh toán không khớp.");

            // 5. Bắt đầu transaction DB để đảm bảo atomicity
            await uow.BeginTransactionAsync(cancellationToken);

            try
            {
                // 5.1 Đánh dấu payment thành công
                payment.MarkAsSuccess(payload.TransactionCode);
                uow.Payments.Update(payment);

                // 5.2 Lấy subscription Active hiện tại của learner (BR-03, BR-04)
                var currentSubscription = await uow.Subscriptions
                    .GetActiveSubscriptionByLearnerIdAsync(payment.LearnerId, cancellationToken);

                var newTier     = payment.PlanSnapshot.TierLevel;
                var currentTier = currentSubscription?.PlanSnapshot.TierLevel ?? 0;

                if (currentSubscription is not null && newTier == currentTier)
                {
                    // BR-03: Gia hạn — cùng tier → kéo dài ExpiredAt của subscription cũ
                    currentSubscription.Extend();
                    uow.Subscriptions.Update(currentSubscription);
                }
                else
                {
                    // BR-04: Nâng cấp — tier cao hơn → Replace subscription cũ, tạo mới
                    if (currentSubscription is not null)
                    {
                        currentSubscription.Replace();
                        uow.Subscriptions.Update(currentSubscription);
                    }

                    // 5.3 Tạo subscription mới từ payment đã thành công
                    var newSubscription = Subscription.Create(payment);
                    await uow.Subscriptions.AddAsync(newSubscription);

                    // 5.4 Cập nhật resource limits theo snapshot của plan mới
                    await ApplySubscriptionQuotaAsync(
                        payment.LearnerId,
                        payment.PlanSnapshot.AiTokenLimit,
                        payment.PlanSnapshot.AiPracticeScenarioLimit,
                        payment.PlanSnapshot.ExpertEvaluationLimit,
                        cancellationToken);
                }

                await uow.CommitTransactionAsync(cancellationToken);
            }
            catch
            {
                await uow.RollbackTransactionAsync(cancellationToken);
                throw;
            }

            return ResponseDto<object>.SuccessResult(new { Message = "Thanh toán xác nhận thành công." });
        }

        /// <summary>
        /// Cập nhật AccountResourceLimit nếu learner có override cá nhân và plan mới
        /// cấp quota lớn hơn. Nếu không có override, quota được đọc từ PlanSnapshot
        /// trong GetSubscriptionResourceUsageQuery (không cần tạo thêm bản ghi).
        /// </summary>
        private async Task ApplySubscriptionQuotaAsync(
            Guid learnerId,
            int aiTokenLimit,
            int aiPracticeLimit,
            int expertEvalLimit,
            CancellationToken cancellationToken)
        {
            var accountLimit = await uow.AccountResourceLimits
                .GetByAccountIdAsync(learnerId, cancellationToken);

            if (accountLimit is null)
                return; // Quota đọc từ PlanSnapshot — không cần tạo override.

            // Chỉ nâng quota nếu plan mới cấp nhiều hơn giá trị override hiện tại.
            var newToken    = Math.Max(accountLimit.AiTokenLimit ?? 0, aiTokenLimit);
            var newPractice = Math.Max(accountLimit.AiPracticeScenarioLimit ?? 0, aiPracticeLimit);
            var newExpert   = Math.Max(accountLimit.ExpertEvaluationRequestLimit ?? 0, expertEvalLimit);

            accountLimit.UpdateLimits(newToken, newPractice, newExpert);
            uow.AccountResourceLimits.Update(accountLimit);
        }
    }
}
