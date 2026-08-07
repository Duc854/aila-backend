using AILA.Application.Common.Dtos.Recommendation;
using AILA.Application.Common.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.Recommendations.Queries.GetRecommendations
{
    public class GetRecommendationsQueryHandler
        : IRequestHandler<GetRecommendationsQuery, List<CourseRecommendationDto>>
    {
        private readonly IRecommendationService _recommendationService;


        public GetRecommendationsQueryHandler(
            IRecommendationService recommendationService)
        {
            _recommendationService = recommendationService;
        }


        public async Task<List<CourseRecommendationDto>> Handle(
            GetRecommendationsQuery request,
            CancellationToken cancellationToken)
        {
            return await _recommendationService
                .GetRecommendationsAsync(
                    request.LearnerId,
                    request.Limit,
                    cancellationToken);
        }
    }
}
