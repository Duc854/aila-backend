using MediatR;
using System;

namespace AILA.Application.Features.Learners.Queries
{
    public class GetOnboardingStatusQuery : IRequest<bool>
    {
        public Guid UserId { get; set; }
    }
}
