using AILA.Application.Features.Quizzes.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Quizzes.Commands.SubmitQuiz
{
    public record SubmitQuizCommand(
        Guid CourseId,
        Guid MaterialId,
        Guid AttemptId,
        Guid LearnerId,
        IReadOnlyList<QuizAnswerSubmissionDto> Answers)
        : IRequest<ResponseDto<QuizResultDto>>;
}
