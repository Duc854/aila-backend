using AILA.Application.Common.Interfaces;
using AILA.Domain.Enums;
using MediatR;
using Shared.Wrappers;
using AILA.Domain.Constants;
using AILA.Domain.Entities;

namespace AILA.Application.Features.Onboarding.Commands.CompleteOnboarding
{
    public class CompleteOnboardingCommand : IRequest<ResponseDto<bool>>
    {
        public Guid UserId { get; set; }
        public LearnerType LearnerType { get; set; }
        public KnowledgeLevel KnowledgeLevel { get; set; }
        public List<Guid> TagIds { get; set; } = new();
    }

    public class CompleteOnboardingCommandHandler : IRequestHandler<CompleteOnboardingCommand, ResponseDto<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CompleteOnboardingCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ResponseDto<bool>> Handle(CompleteOnboardingCommand request, CancellationToken cancellationToken)
        {
            // 1. Load learner
            var learner =
                await _unitOfWork.Learners
                    .GetWithUserAndGoalsAsync(
                        request.UserId,
                        cancellationToken);


            if (learner == null)
            {
                return ResponseDto<bool>.FailResult(
                    "LEARNER_NOT_FOUND",
                    "Không tìm thấy hồ sơ học viên.");
            }



            // 2. Lấy interest tag learner chọn
            var interestTags =
                await _unitOfWork.Tags
                    .GetPublishedByIdsAsync(
                        request.TagIds,
                        cancellationToken);



            if (interestTags.Count != request.TagIds.Count)
            {
                return ResponseDto<bool>.FailResult(
                    "INVALID_TAGS",
                    "Danh sách mục tiêu học tập không hợp lệ.");
            }



            // 3. Lấy system tags
            var systemTags =
                await GetSystemTagsAsync(
                    request.LearnerType,
                    request.KnowledgeLevel,
                    cancellationToken);



            // 4. Bắt đầu transaction khi chuẩn bị ghi
            await _unitOfWork.BeginTransactionAsync(
                cancellationToken);


            try
            {
                // 5. Update learner profile
                learner.CompleteOnboarding(
                    request.LearnerType,
                    request.KnowledgeLevel,
                    interestTags);



                // 6. Tạo LearnerTagScore
                var allTags =
                    interestTags
                        .Concat(systemTags)
                        .DistinctBy(x => x.Id)
                        .ToList();



                foreach (var tag in allTags)
                {
                    var learnerTagScore =
                        new LearnerTagScore(
                            learner.UserId,
                            tag.Id,
                            profileSeed: 200);



                    await _unitOfWork
                        .LearnerTagScores
                        .AddAsync(learnerTagScore);
                }



                // 7. Save + Commit
                await _unitOfWork.CommitTransactionAsync(
                    cancellationToken);


                return ResponseDto<bool>
                    .SuccessResult(true);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(
                    cancellationToken);

                throw;
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


            return await _unitOfWork.Tags
                .GetByCodesAsync(
                    codes,
                    cancellationToken);
        }
    }
}
