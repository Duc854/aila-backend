namespace AILA.Application.Features.QuizMaterials.Dtos;

public sealed record CreateQuizMaterialRequest(
    Guid ModuleId,
    string Title,
    int TimeLimitMinutes,
    decimal PassingScore,
    bool ShowCorrectAnswersAfterSubmission
);
