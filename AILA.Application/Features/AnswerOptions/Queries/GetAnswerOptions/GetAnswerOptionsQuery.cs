using AILA.Application.Features.AnswerOptions.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.AnswerOptions.Queries.GetAnswerOptions;

public sealed record GetAnswerOptionsQuery(
    Guid QuestionId,
    Guid ExpertId
) : IRequest<ResponseDto<List<AnswerOptionDto>>>;