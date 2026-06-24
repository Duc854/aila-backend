using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Profile.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Profile.Commands.UpdateExpertProfile
{
    public class UpdateExpertProfileCommandHandler(IUnitOfWork uow)
        : IRequestHandler<UpdateExpertProfileCommand, ResponseDto<ExpertProfileDto>>
    {
        public async Task<ResponseDto<ExpertProfileDto>> Handle(UpdateExpertProfileCommand request, CancellationToken ct)
        {
            // --- Input validation ---
            if (string.IsNullOrWhiteSpace(request.FullName))
                return ResponseDto<ExpertProfileDto>.FailResult("VALIDATION_ERROR", "FullName không được để trống.");

            if (!string.IsNullOrEmpty(request.AvatarUrl) && !Uri.IsWellFormedUriString(request.AvatarUrl, UriKind.Absolute))
                return ResponseDto<ExpertProfileDto>.FailResult("VALIDATION_ERROR", "AvatarUrl không hợp lệ.");

            if (request.YearsOfExperience < 0)
                return ResponseDto<ExpertProfileDto>.FailResult("VALIDATION_ERROR", "Số năm kinh nghiệm không được nhỏ hơn 0.");

            // --- Load expert with tracking ---
            var expert = await uow.Experts.GetWithUserAsync(request.UserId, ct);

            if (expert == null)
                return ResponseDto<ExpertProfileDto>.FailResult("EXPERT_NOT_FOUND", "Không tìm thấy thông tin chuyên gia.");

            if (!expert.User.IsActive)
                return ResponseDto<ExpertProfileDto>.FailResult("ACCOUNT_INACTIVE", "Tài khoản đã bị vô hiệu hóa.");

            // --- Apply domain changes ---
            expert.User.UpdateProfile(request.FullName, request.AvatarUrl);
            expert.UpdateProfile(request.Bio, request.Specialty, request.YearsOfExperience);

            await uow.SaveChangesAsync(ct);

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
