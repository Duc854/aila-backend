using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Subscriptions.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Subscriptions.Queries.GetSubscriptionStatistics
{
    public record GetSubscriptionStatisticsQuery(
        DateTime? FromDate = null,
        DateTime? ToDate = null)
        : IRequest<ResponseDto<SubscriptionStatisticsDto>>;

    public class GetSubscriptionStatisticsQueryHandler(IUnitOfWork uow)
        : IRequestHandler<GetSubscriptionStatisticsQuery, ResponseDto<SubscriptionStatisticsDto>>
    {
        public async Task<ResponseDto<SubscriptionStatisticsDto>> Handle(
            GetSubscriptionStatisticsQuery request,
            CancellationToken cancellationToken)
        {
            var stats = await uow.Payments.GetSubscriptionStatisticsAsync(
                request.FromDate,
                request.ToDate,
                cancellationToken);

            return ResponseDto<SubscriptionStatisticsDto>.SuccessResult(stats);
        }
    }
}
