using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Questions.Commands.DeleteQuestion;

public sealed record DeleteQuestionCommand(
    Guid QuestionId,
    Guid ExpertId
) : IRequest<ResponseDto<object>>;