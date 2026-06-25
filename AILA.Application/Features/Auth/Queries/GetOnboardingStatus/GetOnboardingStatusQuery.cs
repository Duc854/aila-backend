using AILA.Application.Common.Interfaces;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Auth.Queries.GetOnboardingStatus
{
    public class GetOnboardingStatusQuery : IRequest<ResponseDto<OnboardingStatusResponse>>
    {
        public Guid UserId { get; set; }
    }

    public class OnboardingStatusResponse
    {
        public bool HasCompletedOnboarding { get; set; }
        public string? LearnerType { get; set; }
        public string? KnowledgeLevel { get; set; }
        public List<Guid> LearningGoalIds { get; set; } = new();
    }

    public class GetOnboardingStatusQueryHandler : IRequestHandler<GetOnboardingStatusQuery, ResponseDto<OnboardingStatusResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetOnboardingStatusQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ResponseDto<OnboardingStatusResponse>> Handle(GetOnboardingStatusQuery request, CancellationToken cancellationToken)
        {
            var learner = await _unitOfWork.Learners.GetReadonlyWithUserAndGoalsAsync(request.UserId, cancellationToken);
            if (learner == null)
                return ResponseDto<OnboardingStatusResponse>.FailResult("LEARNER_NOT_FOUND", "Không tìm thấy hồ sơ học viên.");

            var response = new OnboardingStatusResponse
            {
                HasCompletedOnboarding = learner.HasCompletedOnboarding,
                LearnerType = learner.LearnerType?.ToString(),
                KnowledgeLevel = learner.KnowledgeLevel?.ToString(),
                LearningGoalIds = learner.LearningGoals.Select(t => t.Id).ToList()
            };

            return ResponseDto<OnboardingStatusResponse>.SuccessResult(response);
        }
    }
}
