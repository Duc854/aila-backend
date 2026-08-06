using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.AnswerOptions.Commands.DeleteAnswerOption;

public sealed record DeleteAnswerOptionCommand(
    Guid AnswerOptionId,
    Guid ExpertId
) : IRequest<ResponseDto<object>>;
