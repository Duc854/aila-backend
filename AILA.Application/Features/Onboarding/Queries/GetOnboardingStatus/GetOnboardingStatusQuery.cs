using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Onboarding.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Onboarding.Queries.GetOnboardingStatus
{
    public class GetOnboardingStatusQuery : IRequest<ResponseDto<OnboardingStatusDto>>
    {
        public Guid UserId { get; set; }
    }

    public class GetOnboardingStatusQueryHandler : IRequestHandler<GetOnboardingStatusQuery, ResponseDto<OnboardingStatusDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetOnboardingStatusQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ResponseDto<OnboardingStatusDto>> Handle(GetOnboardingStatusQuery request, CancellationToken cancellationToken)
        {
            var learner = await _unitOfWork.Learners.GetReadonlyWithUserAndGoalsAsync(request.UserId, cancellationToken);
            if (learner == null)
                return ResponseDto<OnboardingStatusDto>.FailResult("LEARNER_NOT_FOUND", "Không tìm thấy hồ sơ học viên.");

            var response = new OnboardingStatusDto
            {
                HasCompletedOnboarding = learner.HasCompletedOnboarding,
                LearnerType = learner.LearnerType?.ToString(),
                KnowledgeLevel = learner.KnowledgeLevel?.ToString(),
                LearningGoalIds = learner.LearningGoals.Select(t => t.Id).ToList()
            };

            return ResponseDto<OnboardingStatusDto>.SuccessResult(response);
        }
    }
}
