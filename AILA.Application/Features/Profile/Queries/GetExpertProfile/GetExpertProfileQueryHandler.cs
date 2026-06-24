using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Profile.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Profile.Queries.GetExpertProfile
{
    public class GetExpertProfileQueryHandler(IUnitOfWork uow)
        : IRequestHandler<GetExpertProfileQuery, ResponseDto<ExpertProfileDto>>
    {
        public async Task<ResponseDto<ExpertProfileDto>> Handle(GetExpertProfileQuery request, CancellationToken ct)
        {
            var expert = await uow.Experts.GetReadonlyWithUserAsync(request.UserId, ct);

            if (expert == null)
                return ResponseDto<ExpertProfileDto>.FailResult("EXPERT_NOT_FOUND", "Không tìm thấy thông tin chuyên gia.");

            if (!expert.User.IsActive)
                return ResponseDto<ExpertProfileDto>.FailResult("ACCOUNT_INACTIVE", "Tài khoản đã bị vô hiệu hóa.");

            var dto = new ExpertProfileDto(
                expert.User.Id,
                expert.User.FullName,
                expert.User.Email,
                expert.User.AvatarUrl,
                expert.User.Role.ToString(),
                new ExpertInfoDto(expert.Bio, expert.Specialty, expert.YearsOfExperience)
            );

            return ResponseDto<ExpertProfileDto>.SuccessResult(dto);
        }
    }
}
