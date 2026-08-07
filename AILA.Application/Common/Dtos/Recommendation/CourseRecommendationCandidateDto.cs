using AILA.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Common.Dtos.Recommendation
{
    public class CourseRecommendationCandidateDto
    {
        public Guid CourseId { get; set; }


        public string Name { get; set; }
            = string.Empty;


        public string? ThumbnailUrl { get; set; }


        public string CategoryName { get; set; }
            = string.Empty;


        public string ExpertName { get; set; }
            = string.Empty;


        public string Level { get; set; }
            = string.Empty;



        public List<CourseTagCandidateDto> Tags { get; set; }
            = new();



        public int EnrollmentCount { get; set; }


        public DateTime CreatedAt { get; set; }
    }
}
