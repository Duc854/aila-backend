using AILA.Application.Common.Dtos;
using AILA.Application.Common.Dtos.Recommendation;
using AILA.Application.Common.Interfaces;
using AILA.Domain.Constants;

namespace AILA.Application.Common.InternalService
{
    public class RecommendationService
        : IRecommendationService
    {
        private readonly IUnitOfWork _unitOfWork;


        public RecommendationService(
            IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }



        public async Task<List<CourseRecommendationDto>>
            GetRecommendationsAsync(
                Guid learnerId,
                int limit = 10,
                CancellationToken cancellationToken = default)
        {

            // 1. Lấy learner preference
            var learnerScores =
                await _unitOfWork.LearnerTagScores
                    .GetForRecommendationAsync(
                        learnerId,
                        RecommendationConstants.MinimumTagScore,
                        cancellationToken);



            // 2. Lấy course candidate
            var courses =
                await _unitOfWork.Courses
                    .GetCoursesForRecommendationAsync(
                        cancellationToken);



            // 3. Normalize learner score
            var learnerTagScores =
                learnerScores
                    .ToDictionary(
                        x => x.TagId,
                        x => Normalize(x.RawScore));



            // 4. Tính relevance
            var candidates =
                courses
                    .Select(course =>
                    {
                        var result =
                            CalculateRelevance(
                                course,
                                learnerTagScores);


                        return new CourseRecommendationInternalDto
                        {
                            Course = course,

                            RelevanceScore =
                                result.Score,

                            MatchedTags =
                                result.Tags
                        };

                    })
                    .Where(x =>
                        x.RelevanceScore > 0)
                    .ToList();



            if (!candidates.Any())
                return new List<CourseRecommendationDto>();



            var selected =
                new List<CourseRecommendationInternalDto>();


            var selectedIds =
                new HashSet<Guid>();



            int relevanceCount =
                (int)Math.Ceiling(limit * 0.5);


            int popularityCount =
                (int)Math.Ceiling(limit * 0.3);


            int newestCount =
                limit -
                relevanceCount -
                popularityCount;



            // ===========================
            // A - Relevance 50%
            // ===========================
            var relevanceCourses =
                candidates
                    .OrderByDescending(
                        x => x.RelevanceScore)
                    .Take(relevanceCount)
                    .ToList();



            selected.AddRange(
                relevanceCourses);



            foreach (var item in relevanceCourses)
            {
                selectedIds.Add(
                    item.Course.CourseId);
            }



            // ===========================
            // B - Popularity 30%
            // ===========================
            var popularCourses =
                candidates
                    .Where(x =>
                        !selectedIds.Contains(
                            x.Course.CourseId))
                    .OrderByDescending(
                        x => x.Course.EnrollmentCount)
                    .ThenByDescending(
                        x => x.RelevanceScore)
                    .Take(popularityCount)
                    .ToList();



            selected.AddRange(
                popularCourses);



            foreach (var item in popularCourses)
            {
                selectedIds.Add(
                    item.Course.CourseId);
            }



            // ===========================
            // C - Newest 20%
            // ===========================
            var now = DateTime.UtcNow;

            var startOfMonth =
                new DateTime(
                    now.Year,
                    now.Month,
                    1,
                    0,
                    0,
                    0,
                    DateTimeKind.Utc);

            var newestCourses =
                candidates
                    .Where(x =>
                        !selectedIds.Contains(
                            x.Course.CourseId)
                        &&
                        x.Course.CreatedAt >= startOfMonth)
                    .OrderByDescending(
                        x => x.Course.CreatedAt)
                    .ThenByDescending(
                        x => x.RelevanceScore)
                    .Take(newestCount)
                    .ToList();


            if (newestCourses.Count < newestCount)
            {
                var missing =
                    newestCount - newestCourses.Count;


                var fallbackCourses =
                    candidates
                        .Where(x =>
                            !selectedIds.Contains(
                                x.Course.CourseId)
                            &&
                            !newestCourses
                                .Any(n =>
                                    n.Course.CourseId ==
                                    x.Course.CourseId))
                        .OrderByDescending(
                            x => x.RelevanceScore)
                        .Take(missing)
                        .ToList();


                newestCourses.AddRange(
                    fallbackCourses);
            }



            selected.AddRange(
                newestCourses);



            return selected
                .Select(MapToDto)
                .ToList();
        }





        private (
            decimal Score,
            List<string> Tags)
            CalculateRelevance(
                CourseRecommendationCandidateDto course,
                Dictionary<Guid, decimal> learnerTags)
        {

            decimal score = 0;


            var matchedTags =
                new List<string>();


            foreach (var tag in course.Tags)
            {
                if (learnerTags.TryGetValue(
                    tag.Id,
                    out var tagScore))
                {
                    score += tagScore;

                    matchedTags.Add(tag.Name);
                }
            }


            return
            (
                Math.Round(score, 4),
                matchedTags
            );
        }





        private decimal Normalize(
            int rawScore)
        {
            return Math.Min(
                Math.Round(
                    rawScore /
                    (decimal)RecommendationConstants.MaxTagScore,
                    4),
                1m);
        }





        private CourseRecommendationDto MapToDto(
            CourseRecommendationInternalDto item)
        {
            return new CourseRecommendationDto
            {
                CourseId =
                    item.Course.CourseId,


                Name =
                    item.Course.Name,


                ThumbnailUrl =
                    item.Course.ThumbnailUrl,


                CategoryName =
                    item.Course.CategoryName,


                ExpertName =
                    item.Course.ExpertName,


                Level =
                    item.Course.Level,


                RecommendationScore =
                    item.RelevanceScore,


                MatchedTags =
                    item.MatchedTags
            };
        }
    }
}