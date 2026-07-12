using AILA.Application.Features.Quizzes.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Quizzes.Commands.StartQuizAttempt
{
    public record StartQuizAttemptCommand(Guid CourseId, Guid MaterialId, Guid LearnerId)
        : IRequest<ResponseDto<StartQuizResponseDto>>;
}
