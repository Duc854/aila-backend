using AILA.Domain.Enums;
using MediatR;
using System;
using System.Collections.Generic;

namespace AILA.Application.Features.Learners.Commands
{
    public class CompleteOnboardingCommand : IRequest<Unit>
    {
        public Guid UserId { get; set; }
        public LearnerType LearnerType { get; set; }
        public KnowledgeLevel KnowledgeLevel { get; set; }
        public List<Guid> TagIds { get; set; } = new();
    }
}
