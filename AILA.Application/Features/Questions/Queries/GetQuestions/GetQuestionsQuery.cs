using AILA.Application.Features.Questions.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Questions.Queries.GetQuestions;

public sealed record GetQuestionsQuery(
    Guid QuizMaterialId,
    Guid ExpertId
) : IRequest<ResponseDto<List<QuestionDto>>>;