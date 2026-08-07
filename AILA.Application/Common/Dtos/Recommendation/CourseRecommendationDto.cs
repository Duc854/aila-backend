using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Common.Dtos.Recommendation
{
    public class CourseRecommendationDto
    {
        public Guid CourseId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? ThumbnailUrl { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public string ExpertName { get; set; } = string.Empty;

        public string Level { get; set; } = string.Empty;


        /// <summary>
        /// Điểm phù hợp với learner
        /// </summary>
        public decimal RecommendationScore { get; set; }


        /// <summary>
        /// Những tag khiến course được đề xuất
        /// </summary>
        public List<string> MatchedTags { get; set; }
            = new();
    }
}
