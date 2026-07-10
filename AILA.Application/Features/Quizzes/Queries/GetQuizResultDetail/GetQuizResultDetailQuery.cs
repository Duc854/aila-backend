using AILA.Application.Features.Quizzes.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Quizzes.Queries.GetQuizResultDetail
{
    public record GetQuizResultDetailQuery(Guid CourseId, Guid MaterialId, Guid LearnerId)
        : IRequest<ResponseDto<QuizResultDetailDto>>;
}
