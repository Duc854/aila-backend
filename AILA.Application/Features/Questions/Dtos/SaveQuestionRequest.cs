using AILA.Domain.Enums;

namespace AILA.Application.Features.Questions.Dtos;

public sealed class SaveQuestionRequest
{
    public string Content { get; set; } = string.Empty;

    public QuestionType QuestionType { get; set; }
}