using AILA.Domain.Enums;

namespace AILA.Application.Features.QuizMaterials.Dtos.BulkCreateQuiz;

public sealed class BulkQuestionDto
{
    public string Content { get; set; } = string.Empty;

    public QuestionType QuestionType { get; set; }

    public List<BulkAnswerDto> Answers { get; set; } = [];
}