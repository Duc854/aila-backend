using AILA.Domain.Enums;

namespace AILA.Application.Features.Questions.Dtos;

public sealed class QuestionDto
{
    public Guid Id { get; init; }

    public Guid QuizMaterialId { get; init; }

    public string Content { get; init; } = string.Empty;

    public QuestionType QuestionType { get; init; }

    public string QuestionTypeName { get; init; } = string.Empty;

    public int OrderIndex { get; init; }

    public int AnswerCount { get; init; }
}