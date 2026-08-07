using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Profile.Dtos;
using AILA.Domain.Constants;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
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
            var tags = await uow.Tags
                .GetPublishedByIdsAsync(
                    request.LearningGoalTagIds,
                    ct);

            if (tags.Count != request.LearningGoalTagIds.Length)
                return ResponseDto<LearnerProfileDto>.FailResult("INVALID_TAGS", "Danh sách mục tiêu học tập không hợp lệ.");

            // --- Apply domain changes ---
            await uow.BeginTransactionAsync(ct);

            try
            {
                learner.User.UpdateProfile(
                    request.FullName,
                    request.AvatarUrl);

                uow.Learners.SetLearnerDetails(
                    learner,
                    request.LearnerType,
                    request.KnowledgeLevel);


                learner.UpdateLearningGoals(tags);


                await SyncLearnerTagScoresAsync(
                    learner,
                    tags,
                    request.LearnerType,
                    request.KnowledgeLevel,
                    ct);


                await uow.CommitTransactionAsync(ct);
            }
            catch
            {
                await uow.RollbackTransactionAsync(ct);
                throw;
            }

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

        private async Task SyncLearnerTagScoresAsync(
            Learner learner,
            List<Tag> interestTags,
            LearnerType learnerType,
            KnowledgeLevel knowledgeLevel,
            CancellationToken ct)
        {
            var existingScores =
                await uow.LearnerTagScores
                    .GetByLearnerIdAsync(
                        learner.UserId,
                        ct);


            var systemTags =
                await GetSystemTagsAsync(
                    learnerType,
                    knowledgeLevel,
                    ct);


            var profileTags =
                interestTags
                    .Concat(systemTags)
                    .DistinctBy(x => x.Id)
                    .ToList();



            foreach (var score in existingScores)
            {
                score.UpdateProfileSeed(0);
            }



            foreach (var tag in profileTags)
            {
                var existing =
                    existingScores
                        .FirstOrDefault(
                            x => x.TagId == tag.Id);


                if (existing != null)
                {
                    existing.UpdateProfileSeed(200);
                }
                else
                {
                    await uow.LearnerTagScores
                        .AddAsync(
                            new LearnerTagScore(
                                learner.UserId,
                                tag.Id,
                                200)
                            );
                }
            }
        }

        private async Task<List<Tag>> GetSystemTagsAsync(
            LearnerType learnerType,
            KnowledgeLevel knowledgeLevel,
            CancellationToken cancellationToken)
        {
            var codes = new List<string>
                {
                    learnerType switch
                    {
                        LearnerType.Student
                            => ReservedTagCodes.Student,

                        LearnerType.OfficeWorker
                            => ReservedTagCodes.OfficeWorker,

                        LearnerType.Freelancer
                            => ReservedTagCodes.Freelancer,

                        LearnerType.BusinessOwner
                            => ReservedTagCodes.BusinessOwner,

                        LearnerType.CivilServant
                            => ReservedTagCodes.CivilServant,

                        LearnerType.Retired
                            => ReservedTagCodes.Retired,

                        _ => throw new ArgumentOutOfRangeException()
                    },

                    knowledgeLevel switch
                    {
                        KnowledgeLevel.Beginner
                            => ReservedTagCodes.Beginner,

                        KnowledgeLevel.Intermediate
                            => ReservedTagCodes.Intermediate,

                        KnowledgeLevel.Advanced
                            => ReservedTagCodes.Advanced,

                        _ => throw new ArgumentOutOfRangeException()
                    }
                };


            return await uow.Tags
                .GetByCodesAsync(
                    codes,
                    cancellationToken);
        }
    }
}
