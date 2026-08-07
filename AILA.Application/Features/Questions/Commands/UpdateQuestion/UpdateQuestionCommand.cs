using AILA.Application.Features.Questions.Dtos;
using AILA.Domain.Enums;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Questions.Commands.UpdateQuestion;

public sealed record UpdateQuestionCommand(
    Guid QuestionId,
    Guid ExpertId,
    string Content,
    QuestionType QuestionType
) : IRequest<ResponseDto<QuestionDto>>;
