using AILA.Application.Features.Questions.Dtos;
using AILA.Domain.Enums;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Questions.Commands.CreateQuestion;

public sealed record CreateQuestionCommand(
    Guid QuizMaterialId,
    Guid ExpertId,
    string Content,
    QuestionType QuestionType
) : IRequest<ResponseDto<QuestionDto>>;
