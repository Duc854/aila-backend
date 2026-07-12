using AILA.Application.Features.Profile.Dtos;
using AILA.Domain.Enums;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Profile.Commands.UpdateLearnerProfile
{
    public record UpdateLearnerProfileCommand(
        Guid UserId,
        string FullName,
        string? AvatarUrl,
        LearnerType? LearnerType,
        KnowledgeLevel? KnowledgeLevel,
        Guid[] LearningGoalTagIds
    ) : IRequest<ResponseDto<LearnerProfileDto>>;
}
