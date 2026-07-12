using AILA.Application.Features.Quizzes.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Quizzes.Queries.GetQuizResultSummary
{
    public record GetQuizResultSummaryQuery(Guid CourseId, Guid MaterialId, Guid LearnerId)
        : IRequest<ResponseDto<QuizResultSummaryDto>>;
}
