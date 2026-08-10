using AILA.Application.Common.Dtos.Recommendation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Common.Interfaces
{
    public interface IRecommendationService
    {
        Task<List<CourseRecommendationDto>>
            GetRecommendationsAsync(
                Guid learnerId,
                int limit = 10,
                CancellationToken cancellationToken = default);
    }
}
