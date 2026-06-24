using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Profile.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Profile.Queries.GetLearnerProfile
{
    public class GetLearnerProfileQueryHandler(IUnitOfWork uow)
        : IRequestHandler<GetLearnerProfileQuery, ResponseDto<LearnerProfileDto>>
    {
        public async Task<ResponseDto<LearnerProfileDto>> Handle(GetLearnerProfileQuery request, CancellationToken ct)
        {
            var learner = await uow.Learners.GetReadonlyWithUserAndGoalsAsync(request.UserId, ct);

            if (learner == null)
                return ResponseDto<LearnerProfileDto>.FailResult("LEARNER_NOT_FOUND", "Không tìm thấy thông tin học viên.");

            if (!learner.User.IsActive)
                return ResponseDto<LearnerProfileDto>.FailResult("ACCOUNT_INACTIVE", "Tài khoản đã bị vô hiệu hóa.");

            var dto = new LearnerProfileDto(
                learner.User.Id,
                learner.User.FullName,
                learner.User.Email,
                learner.User.AvatarUrl,
                learner.User.Role.ToString(),
                new LearnerInfoDto(
                    learner.LearnerType?.ToString(),
                    learner.KnowledgeLevel?.ToString(),
                    learner.HasCompletedOnboarding,
                    learner.LearningGoals.Select(t => new TagDto(t.Id, t.Name))
                )
            );

            return ResponseDto<LearnerProfileDto>.SuccessResult(dto);
        }
    }
}
