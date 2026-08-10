namespace AILA.Application.Features.QuizMaterials.Dtos;

public sealed class QuizMaterialDto
{
    public Guid MaterialId { get; init; }

    public string Title { get; init; } = string.Empty;

    public int TimeLimitMinutes { get; init; }

    public decimal PassingScore { get; init; }

    public bool ShowCorrectAnswersAfterSubmission { get; init; }
}
