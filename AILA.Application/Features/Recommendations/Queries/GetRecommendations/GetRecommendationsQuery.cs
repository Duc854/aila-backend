using AILA.Application.Common.Dtos.Recommendation;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.Recommendations.Queries.GetRecommendations
{
    public record GetRecommendationsQuery(
        Guid LearnerId,
        int Limit = 10
    ) : IRequest<List<CourseRecommendationDto>>;
}
