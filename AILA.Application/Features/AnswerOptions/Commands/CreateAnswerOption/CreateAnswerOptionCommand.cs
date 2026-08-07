using AILA.Application.Features.AnswerOptions.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.AnswerOptions.Commands.CreateAnswerOption;

public sealed record CreateAnswerOptionCommand(
    Guid QuestionId,
    Guid ExpertId,
    string Content,
    bool IsCorrect
) : IRequest<ResponseDto<AnswerOptionDto>>;
