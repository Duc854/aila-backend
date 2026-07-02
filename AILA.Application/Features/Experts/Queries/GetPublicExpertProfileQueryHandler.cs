using AILA.Application.Common.Dtos;
using AILA.Application.Common.Interfaces;
using MediatR;

namespace AILA.Application.Features.Experts.Queries
{
    public class GetPublicExpertProfileQueryHandler
        : IRequestHandler<GetPublicExpertProfileQuery, PublicExpertProfileDto?>
    {
        private readonly IUnitOfWork _uow;

        public GetPublicExpertProfileQueryHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<PublicExpertProfileDto?> Handle(
            GetPublicExpertProfileQuery request,
            CancellationToken cancellationToken)
        {
            // Expert không tồn tại hoặc tài khoản liên kết đã bị vô hiệu hóa → không hiển thị (404)
            var expert = await _uow.Experts.GetReadonlyWithUserAsync(request.ExpertId, cancellationToken);
            if (expert is null || !expert.User.IsActive)
                return null;

            var courses = await _uow.Courses.GetPublishedByExpertAsync(request.ExpertId, cancellationToken);

            var courseDtos = courses.Select(c => new PublicCourseDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                ThumbnailUrl = c.ThumbnailUrl,
                Level = c.Level.ToString(),
                DurationHours = c.DurationHours
            }).ToList();

            return new PublicExpertProfileDto
            {
                Id = expert.UserId,
                FullName = expert.User.FullName,
                AvatarUrl = expert.User.AvatarUrl,
                Bio = expert.Bio,
                Specialty = expert.Specialty,
                YearsOfExperience = expert.YearsOfExperience,
                Courses = courseDtos,
                TotalPublishedCourses = courseDtos.Count
            };
        }
    }
}
