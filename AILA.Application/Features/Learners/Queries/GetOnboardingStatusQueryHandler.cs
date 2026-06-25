using AILA.Application.Common.Interfaces;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Features.Learners.Queries
{
    public class GetOnboardingStatusQueryHandler : IRequestHandler<GetOnboardingStatusQuery, bool>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetOnboardingStatusQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(GetOnboardingStatusQuery request, CancellationToken cancellationToken)
        {
            var learner = await _unitOfWork.Learners.GetByIdAsync(request.UserId);
            
            if (learner == null)
            {
                throw new UnauthorizedAccessException("Learner không tồn tại.");
            }

            return learner.HasCompletedOnboarding;
        }
    }
}
