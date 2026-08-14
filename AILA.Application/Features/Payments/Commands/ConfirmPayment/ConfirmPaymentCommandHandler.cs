using AILA.Application.Common.Interfaces;
using AILA.Domain.Entities;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Payments.Commands.ConfirmPayment
{
    /// <summary>
    /// UC-19 Steps 4–7: Xử lý webhook SePay → xác nhận payment → áp dụng subscription policy.
    /// BR-03: Khi purchase cùng tier → tạo subscription mới, không extend.
    /// BR-04: Khi upgrade (tier cao hơn) → Replace subscription cũ, tạo mới.
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
            if (!sePayService.VerifyWebhookSignature(request.RawBody, request.Signature, request.Timestamp))
                return ResponseDto<object>.FailResult(
                    PaymentErrors.InvalidSignature,
                    "Chữ ký webhook không hợp lệ.");

            var payload = request.Payload;

            // Chỉ xử lý giao dịch tiền vào (transferType = "in")
            if (!string.Equals(payload.TransferType, "in", StringComparison.OrdinalIgnoreCase))
                return ResponseDto<object>.SuccessResult(new { Message = "Bỏ qua — không phải giao dịch tiền vào." });

            // 2. Tìm payment theo OrderCode (khớp với Content của giao dịch)
            // SePay gửi nội dung chuyển khoản trong field Content — chứa OrderCode hệ thống
            var orderCode = ExtractOrderCode(payload.Content ?? payload.Code ?? string.Empty);

            var payment = await uow.Payments.GetByOrderCodeAsync(orderCode, cancellationToken);

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
            // SePay gửi transferAmount — so sánh với amount đã lưu (cho phép sai lệch nhỏ do làm tròn)
            if (Math.Abs(payload.TransferAmount - payment.Amount) > 0.01m)
                return ResponseDto<object>.FailResult(
                    PaymentErrors.AmountMismatch,
                    "Số tiền thanh toán không khớp.");

            // 5. Bắt đầu transaction DB để đảm bảo atomicity
            await uow.BeginTransactionAsync(cancellationToken);

            try
            {
                // 5.1 Đánh dấu payment thành công
                // SePay dùng referenceCode là mã giao dịch ngân hàng
                var transactionCode = payload.ReferenceCode
                    ?? payload.Id.ToString();
                payment.MarkAsSuccess(transactionCode);
                uow.Payments.Update(payment);

                // 5.2 Lấy subscription Active hiện tại của learner (BR-03, BR-04)
                var currentSubscription = await uow.Subscriptions
                    .GetActiveSubscriptionByLearnerIdAsync(payment.LearnerId, cancellationToken);

                var newTier     = payment.PlanSnapshot.TierLevel;
                var currentTier = currentSubscription?.PlanSnapshot.TierLevel ?? 0;

                if (currentSubscription is not null && newTier == currentTier)
                {
                    // BR-03: Cùng tier → Replace subscription cũ, tạo subscription mới
                    // (không extend subscription hiện tại)
                    currentSubscription.Replace();
                    uow.Subscriptions.Update(currentSubscription);
                    
                    // Tạo subscription mới
                    var newSubscription = Subscription.Create(payment);
                    await uow.Subscriptions.AddAsync(newSubscription);
                }
                else
                {
                    // BR-04: Nâng cấp — tier cao hơn → Replace subscription cũ, tạo mới
                    if (currentSubscription is not null)
                    {
                        currentSubscription.Replace();
                        uow.Subscriptions.Update(currentSubscription);
                    }

                    // Tạo subscription mới từ payment đã thành công
                    var newSubscription = Subscription.Create(payment);
                    await uow.Subscriptions.AddAsync(newSubscription);
                }

                // Cập nhật resource limits theo snapshot của plan mới
                await ApplySubscriptionQuotaAsync(
                    payment.LearnerId,
                    payment.PlanSnapshot.AiTokenLimit,
                    payment.PlanSnapshot.AiPracticeScenarioLimit,
                    payment.PlanSnapshot.ExpertEvaluationLimit,
                    cancellationToken);

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
        /// Trích xuất OrderCode từ nội dung chuyển khoản.
        /// Nội dung CK có thể là "AILA1705300600000" hoặc có text thừa như "Chuyen tien AILA1705300600000"
        /// → tìm pattern "AILA" + digits.
        /// </summary>
        private static string ExtractOrderCode(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return string.Empty;

            // Khớp pattern SePay: tiền tố "AILA" + 3–10 chữ số (theo cấu hình cấu trúc mã)
            // Ví dụ: "AILA1723280400" hoặc "Chuyen tien AILA1723280400"
            var match = System.Text.RegularExpressions.Regex.Match(
                content,
                @"AILA\d{3,10}",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            return match.Success ? match.Value.ToUpperInvariant() : content.Trim();
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
