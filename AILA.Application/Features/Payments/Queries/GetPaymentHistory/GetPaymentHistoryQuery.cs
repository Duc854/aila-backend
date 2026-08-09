using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Payments.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Payments.Queries.GetPaymentHistory
{
    /// <summary>
    /// UC-20 Steps 1–4: Learner xem lịch sử thanh toán với filter ngày và phân trang.
    /// BR-01: Chỉ xem lịch sử của chính mình.
    /// BR-02: Có thể lọc theo date range.
    /// </summary>
    public record GetPaymentHistoryQuery(
        Guid LearnerId,
        DateTime? FromDate,
        DateTime? ToDate,
        int Page,
        int PageSize
    ) : IRequest<ResponseDto<PaymentHistoryDto>>;

    public class GetPaymentHistoryQueryHandler(IUnitOfWork uow)
        : IRequestHandler<GetPaymentHistoryQuery, ResponseDto<PaymentHistoryDto>>
    {
        private const int DefaultPageSize = 10;
        private const int MaxPageSize     = 50;

        public async Task<ResponseDto<PaymentHistoryDto>> Handle(
            GetPaymentHistoryQuery request,
            CancellationToken cancellationToken)
        {
            // Validate pagination
            var page     = Math.Max(1, request.Page);
            var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

            // Validate date range (BR-02)
            if (request.FromDate.HasValue && request.ToDate.HasValue
                && request.FromDate > request.ToDate)
                return ResponseDto<PaymentHistoryDto>.FailResult(
                    PaymentErrors.InvalidDateRange,
                    "Ngày bắt đầu không được lớn hơn ngày kết thúc.");

            var (items, totalCount) = await uow.Payments.GetPaymentHistoryAsync(
                request.LearnerId,
                request.FromDate,
                request.ToDate,
                page,
                pageSize,
                cancellationToken);

            // AF-01: Không có giao dịch phù hợp → trả mảng rỗng, không báo lỗi.
            var historyItems = items.Select(p => new PaymentHistoryItemDto(
                Id: p.Id,
                SubscriptionPlanName: p.PlanSnapshot.TierLevel > 0
                    ? (p.SubscriptionPlan?.Name ?? $"Gói Tier {p.PlanSnapshot.TierLevel}")
                    : "Gói đăng ký",
                Amount: p.Amount,
                Status: p.Status.ToString(),
                PaidAt: p.PaidAt,
                CreatedAt: p.CreatedAt));

            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var dto = new PaymentHistoryDto(
                Items: historyItems,
                TotalCount: totalCount,
                Page: page,
                PageSize: pageSize,
                TotalPages: totalPages);

            return ResponseDto<PaymentHistoryDto>.SuccessResult(dto);
        }
    }
}
