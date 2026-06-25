using AILA.Application.Common.Interfaces;
using AILA.Domain.Enums;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Auth.Commands.CompleteOnboarding
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
            var learner = await _unitOfWork.Learners.GetWithUserAndGoalsAsync(request.UserId, cancellationToken);
            if (learner == null)
                return ResponseDto<bool>.FailResult("LEARNER_NOT_FOUND", "Không tìm thấy hồ sơ học viên.");

            var tags = await _unitOfWork.Tags.GetByIdsAsync(request.TagIds, cancellationToken);
            if (!tags.Any())
                return ResponseDto<bool>.FailResult("INVALID_TAGS", "Danh sách mục tiêu học tập không hợp lệ.");

            learner.CompleteOnboarding(request.LearnerType, request.KnowledgeLevel, tags);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ResponseDto<bool>.SuccessResult(true);
        }
    }
}
