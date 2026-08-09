using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Application.Features.Payments.Dtos;
using AILA.Domain.Entities;
using AILA.Domain.ValueObjects;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Payments.Commands.CreatePayment
{
    /// <summary>
    /// UC-19 Steps 1–2: Kiểm tra điều kiện mua, tạo Payment và trả về thông tin QR SePay.
    /// BR-01: Một learner chỉ có một gói Active tại một thời điểm.
    /// BR-02: Snapshot plan được chụp tại thời điểm tạo payment.
    /// BR-04: Gói tier cao hơn thay thế gói hiện tại.
    /// BR-05: Không được mua gói tier thấp hơn gói đang Active.
    /// AF-01: Plan không khả dụng → báo lỗi, không tạo payment.
    /// </summary>
    public class CreatePaymentCommandHandler(
        IUnitOfWork uow,
        ISePayService sePayService) 
        : IRequestHandler<CreatePaymentCommand, ResponseDto<CreatePaymentResultDto>>
    {
        // Thời gian hết hạn QR: 15 phút
        private const int PaymentExpiryMinutes = 15;

        public async Task<ResponseDto<CreatePaymentResultDto>> Handle(
            CreatePaymentCommand request,
            CancellationToken cancellationToken)
        {
            // 1. Kiểm tra gói đăng ký được chọn còn Active không (AF-01)
            var plan = await uow.SubscriptionPlans.GetByIdReadOnlyAsync(
                request.SubscriptionPlanId, cancellationToken);

            if (plan is null)
                return ResponseDto<CreatePaymentResultDto>.FailResult(
                    PaymentErrors.PlanNotFound,
                    "Gói đăng ký không tồn tại.");

            if (!plan.IsActive())
                return ResponseDto<CreatePaymentResultDto>.FailResult(
                    PaymentErrors.PlanNotAvailable,
                    "Gói đăng ký đã ngừng bán, vui lòng chọn gói khác.");

            // 2. Lấy gói đang Active của learner (nếu có) để áp dụng BR-04, BR-05
            var currentSubscription = await uow.Subscriptions
                .GetActiveSubscriptionByLearnerIdAsync(request.LearnerId, cancellationToken);

            if (currentSubscription is not null)
            {
                var currentTier = currentSubscription.PlanSnapshot.TierLevel;

                // BR-05: Không được mua gói tier thấp hơn
                if (plan.TierLevel < currentTier)
                    return ResponseDto<CreatePaymentResultDto>.FailResult(
                        PaymentErrors.LowerTierNotAllowed,
                        "Không thể mua gói có cấp độ thấp hơn gói đang hoạt động. " +
                        "Vui lòng chọn gói cùng hoặc cao hơn để gia hạn / nâng cấp.");
            }

            // 3. Huỷ payment Pending cũ nếu có (tránh duplicate giao dịch treo)
            var pendingPayment = await uow.Payments.GetPendingPaymentByLearnerIdAsync(
                request.LearnerId, cancellationToken);

            if (pendingPayment is not null)
            {
                pendingPayment.Cancel();
                uow.Payments.Update(pendingPayment);
            }

            // 4. Tạo snapshot plan tại thời điểm mua (BR-02)
            var snapshot = new SubscriptionPlanSnapshot(
                plan.TierLevel,
                plan.DurationInDays,
                plan.AiTokenLimit,
                plan.AiPracticeScenarioLimit,
                plan.ExpertEvaluationLimit);

            // 5. Tạo orderCode và nội dung chuyển khoản
            var orderCode    = GenerateOrderCode();
            var description  = $"AILA {plan.Name}";
            var expiredAt    = DateTime.UtcNow.AddMinutes(PaymentExpiryMinutes);

            var payment = new Payment(
                learnerId: request.LearnerId,
                subscriptionPlanId: request.SubscriptionPlanId,
                amount: plan.Price,
                orderCode: orderCode,
                paymentContent: $"AILA {orderCode}",
                expiredAt: expiredAt,
                planSnapshot: snapshot);

            await uow.Payments.AddAsync(payment);
            await uow.SaveChangesAsync(cancellationToken);

            // 6. Lấy thông tin SePay QR (không ném exception ra ngoài transaction)
            var sePayInfo = sePayService.CreatePaymentInfo(
                orderCode,
                plan.Price,
                description);

            var result = new CreatePaymentResultDto(
                PaymentId: payment.Id,
                OrderCode: payment.OrderCode,
                PaymentContent: payment.PaymentContent,
                Amount: payment.Amount,
                QrCodeUrl: sePayInfo.QrCodeUrl,
                BankAccountNumber: sePayInfo.BankAccountNumber,
                BankAccountName: sePayInfo.BankAccountName,
                BankName: sePayInfo.BankName,
                ExpiredAt: payment.ExpiredAt);

            return ResponseDto<CreatePaymentResultDto>.SuccessResult(result);
        }

        /// <summary>
        /// Tạo mã đơn hàng nội bộ ngắn gọn, duy nhất, dễ đọc trên QR.
        /// Format: AILA + Timestamp(ms) → đảm bảo đủ ngắn để nằm trong nội dung CK.
        /// </summary>
        private static string GenerateOrderCode()
        {
            // Dùng ticks để đảm bảo tính duy nhất trong môi trường single-instance.
            // Trong môi trường multi-instance, nên dùng distributed ID (Snowflake, ULID).
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return $"AILA{timestamp}";
        }
    }
}
