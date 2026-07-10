using AILA.Application.Features.AnswerOptions.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.AnswerOptions.Commands.ReorderAnswerOptions;

public sealed record ReorderAnswerOptionsCommand(
    Guid QuestionId,
    Guid ExpertId,
    List<AnswerOptionOrderItem> Items
) : IRequest<ResponseDto<object>>;