using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Experts.Dtos;
using AILA.Domain.Enums;
using MediatR;
using Shared.Wrappers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Features.Experts.Queries.GetExpertAiResourceUsage
{
    public record GetExpertAiResourceUsageQuery(Guid ExpertId) 
        : IRequest<ResponseDto<ExpertAiResourceUsageDto>>;

    public class GetExpertAiResourceUsageQueryHandler 
        : IRequestHandler<GetExpertAiResourceUsageQuery, ResponseDto<ExpertAiResourceUsageDto>>
    {
        private readonly IUnitOfWork _uow;

        public GetExpertAiResourceUsageQueryHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<ResponseDto<ExpertAiResourceUsageDto>> Handle(
            GetExpertAiResourceUsageQuery request, 
            CancellationToken cancellationToken)
        {
            // 1. Lấy thông tin hạn mức ghi đè riêng của tài khoản Expert (nếu có) - BR-02
            var accountLimit = await _uow.Subscriptions.GetResourceLimitByAccountIdAsync(
                request.ExpertId, cancellationToken);

            // 2. Lấy chính sách hạn mức mặc định cho tài khoản Expert từ cấu hình nền tảng - BR-02
            var defaultPolicy = await _uow.Subscriptions.GetDefaultPolicyAsync(
                ResourceAccountType.Expert, cancellationToken);

            // 3. Lấy dữ liệu tiêu thụ tài nguyên thực tế của Expert - BR-01
            var usage = await _uow.Subscriptions.GetResourceUsageByAccountIdAsync(
                request.ExpertId, cancellationToken);

            // Xác định hạn mức AI Token được cấp (Allocated Amount)
            int allocatedTokens = accountLimit?.AiTokenLimit 
                ?? defaultPolicy?.AiTokenLimit 
                ?? 100000;

            // Số lượng đã sử dụng (Consumed Amount)
            int consumedTokens = usage?.AiTokenUsed ?? 0;

            // Số lượng còn lại (Remaining Amount)
            int remainingTokens = Math.Max(0, allocatedTokens - consumedTokens);

            // Tính tỷ lệ phần trăm đã dùng
            decimal usagePercentage = allocatedTokens > 0 
                ? Math.Round(((decimal)consumedTokens / allocatedTokens) * 100, 2) 
                : 0.00m;

            bool isExceeded = consumedTokens >= allocatedTokens;
            bool isNearLimit = usagePercentage >= 80.00m;

            string statusMessage = isExceeded
                ? "Bạn đã sử dụng hết hạn mức AI Token được cấp. Vui lòng liên hệ Quản trị viên để bổ sung."
                : isNearLimit
                    ? $"Cảnh báo: Bạn đã sử dụng {usagePercentage}% hạn mức AI Token được cấp."
                    : "Hạn mức AI Token hoạt động bình thường.";

            var dto = new ExpertAiResourceUsageDto
            {
                AccountId = request.ExpertId,
                AllocatedTokens = allocatedTokens,
                ConsumedTokens = consumedTokens,
                RemainingTokens = remainingTokens,
                UsagePercentage = usagePercentage,
                IsNearLimit = isNearLimit,
                IsExceeded = isExceeded,
                StatusMessage = statusMessage
            };

            return ResponseDto<ExpertAiResourceUsageDto>.SuccessResult(dto);
        }
    }
}
