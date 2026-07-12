using AILA.Application.Features.Questions.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Questions.Commands.ReorderQuestions;

public sealed record ReorderQuestionsCommand(
    Guid QuizMaterialId,
    Guid ExpertId,
    List<QuestionOrderItem> Items
) : IRequest<ResponseDto<object>>;