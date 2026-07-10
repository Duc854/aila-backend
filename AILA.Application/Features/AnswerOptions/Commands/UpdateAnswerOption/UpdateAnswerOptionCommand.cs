using AILA.Application.Features.AnswerOptions.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.AnswerOptions.Commands.UpdateAnswerOption;

public sealed record UpdateAnswerOptionCommand(
    Guid AnswerOptionId,
    Guid ExpertId,
    string Content,
    bool IsCorrect
) : IRequest<ResponseDto<AnswerOptionDto>>;