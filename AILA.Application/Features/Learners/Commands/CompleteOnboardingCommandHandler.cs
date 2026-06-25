using AILA.Application.Common.Interfaces;
using AILA.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Features.Learners.Commands
{
    public class CompleteOnboardingCommandHandler : IRequestHandler<CompleteOnboardingCommand, Unit>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CompleteOnboardingCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Unit> Handle(CompleteOnboardingCommand request, CancellationToken cancellationToken)
        {
            var learner = await _unitOfWork.Learners.GetByIdAsync(request.UserId);
            if (learner == null)
            {
                throw new UnauthorizedAccessException("Learner không tồn tại.");
            }

            var selectedTags = new List<Tag>();
            foreach (var tagId in request.TagIds)
            {
                var tag = await _unitOfWork.Tags.GetByIdAsync(tagId);
                if (tag != null)
                {
                    selectedTags.Add(tag);
                }
            }

            // Domain rule will check inside CompleteOnboarding (e.g., must be published, >0 tags)
            learner.CompleteOnboarding(request.LearnerType, request.KnowledgeLevel, selectedTags);

            _unitOfWork.Learners.Update(learner);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
