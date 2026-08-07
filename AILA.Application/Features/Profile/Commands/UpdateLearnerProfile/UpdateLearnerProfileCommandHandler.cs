using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Profile.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Profile.Commands.UpdateLearnerProfile
{
    public class UpdateLearnerProfileCommandHandler(IUnitOfWork uow)
        : IRequestHandler<UpdateLearnerProfileCommand, ResponseDto<LearnerProfileDto>>
    {
        public async Task<ResponseDto<LearnerProfileDto>> Handle(UpdateLearnerProfileCommand request, CancellationToken ct)
        {
            // --- Input validation ---
            if (string.IsNullOrWhiteSpace(request.FullName))
                return ResponseDto<LearnerProfileDto>.FailResult("VALIDATION_ERROR", "FullName không được để trống.");

            if (!string.IsNullOrEmpty(request.AvatarUrl) && !Uri.IsWellFormedUriString(request.AvatarUrl, UriKind.Absolute))
                return ResponseDto<LearnerProfileDto>.FailResult("VALIDATION_ERROR", "AvatarUrl không hợp lệ.");

            if (request.LearningGoalTagIds == null || request.LearningGoalTagIds.Length == 0)
                return ResponseDto<LearnerProfileDto>.FailResult("VALIDATION_ERROR", "Danh sách mục tiêu không được để trống.");

            // --- Load learner with tracking ---
            var learner = await uow.Learners.GetWithUserAndGoalsAsync(request.UserId, ct);

            if (learner == null)
                return ResponseDto<LearnerProfileDto>.FailResult("LEARNER_NOT_FOUND", "Không tìm thấy thông tin học viên.");

            if (!learner.User.IsActive)
                return ResponseDto<LearnerProfileDto>.FailResult("ACCOUNT_INACTIVE", "Tài khoản đã bị vô hiệu hóa.");

            // --- Fetch and validate tags ---
            var tags = await uow.Tags.GetByIdsAsync(request.LearningGoalTagIds, ct);

            if (tags.Count != request.LearningGoalTagIds.Length)
                return ResponseDto<LearnerProfileDto>.FailResult("TAG_NOT_FOUND", "Một hoặc nhiều tag không tồn tại.");

            if (tags.Any(t => !t.IsPublished))
                return ResponseDto<LearnerProfileDto>.FailResult("UNPUBLISHED_TAG", "Có mục tiêu chứa tag chưa được phê duyệt.");

            // --- Apply domain changes ---
            learner.User.UpdateProfile(request.FullName, request.AvatarUrl);
            uow.Learners.SetLearnerDetails(learner, request.LearnerType, request.KnowledgeLevel);
            learner.UpdateLearningGoals(tags);

            await uow.SaveChangesAsync(ct);

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
                ),
                Enumerable.Empty<EnrollmentSummaryDto>(),
                // Màn cập nhật hồ sơ không tổng hợp dashboard học tập (UC-30) — trả về giá trị rỗng.
                new LearningSummaryDto(0, 0, 0, 0, 0, null, 0, null),
                Enumerable.Empty<QuizHistoryItemDto>(),
                Enumerable.Empty<AiScenarioHistoryItemDto>()
            );

            return ResponseDto<LearnerProfileDto>.SuccessResult(dto);
        }
    }
}
