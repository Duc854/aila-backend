using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Common.Dtos.Recommendation
{
    public class CourseRecommendationInternalDto
    {
        public CourseRecommendationCandidateDto Course { get; set; }
            = null!;


        public decimal RelevanceScore { get; set; }


        public List<string> MatchedTags { get; set; }
            = new();
    }
}
