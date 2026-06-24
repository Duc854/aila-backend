using AILA.Application.Common.Dtos;
using AILA.Application.Common.Interfaces;
using AILA.Domain.Enums;
using MediatR;

namespace AILA.Application.Features.Experts.Queries
{
    public class GetExpertProfileQueryHandler
        : IRequestHandler<GetExpertProfileQuery, ExpertProfileDto?>
    {
        private readonly IUnitOfWork _uow;

        public GetExpertProfileQueryHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<ExpertProfileDto?> Handle(
            GetExpertProfileQuery request,
            CancellationToken     cancellationToken)
        {
            // Lấy User kèm Expert profile trong 1 lần truy vấn (Eager Loading)
            var user = await _uow.Users.GetExpertWithProfileAsync(request.ExpertId);

            // Không tìm thấy hoặc không phải role Expert → trả về null
            if (user is null || user.Role != UserRole.Expert || user.Expert is null)
                return null;

            return new ExpertProfileDto
            {
                UserId            = user.Id,
                FullName          = user.FullName,
                Email             = user.Email,
                AvatarUrl         = user.AvatarUrl,
                Bio               = user.Expert.Bio,
                Specialty         = user.Expert.Specialty,
                YearsOfExperience = user.Expert.YearsOfExperience
            };
        }
    }
}
