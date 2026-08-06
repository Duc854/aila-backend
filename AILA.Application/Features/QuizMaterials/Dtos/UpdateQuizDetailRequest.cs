namespace AILA.Application.Features.QuizMaterials.Dtos;

public sealed class UpdateQuizDetailRequest
{
    public int TimeLimitMinutes { get; set; }

    public decimal PassingScore { get; set; }

    public bool ShowCorrectAnswersAfterSubmission { get; set; }
}
