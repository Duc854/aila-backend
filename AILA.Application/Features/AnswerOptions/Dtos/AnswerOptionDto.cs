namespace AILA.Application.Features.AnswerOptions.Dtos;

public sealed class AnswerOptionDto
{
    public Guid Id { get; init; }

    public Guid QuestionId { get; init; }

    public string Content { get; init; } = string.Empty;

    public bool IsCorrect { get; init; }

    public int OrderIndex { get; init; }
}
