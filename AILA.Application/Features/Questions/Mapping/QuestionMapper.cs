using AILA.Application.Features.Questions.Dtos;
using AILA.Domain.Entities;

namespace AILA.Application.Features.Questions.Mapping;

internal static class QuestionMapper
{
    internal static QuestionDto MapToDto(
        Question question)
    {
        return new QuestionDto
        {
            Id = question.Id,

            QuizMaterialId = question.QuizMaterialId,

            Content = question.Content,

            QuestionType = question.QuestionType,

            QuestionTypeName =
                question.QuestionType.ToString(),

            OrderIndex = question.OrderIndex,

            AnswerCount =
                question.AnswerOptions.Count
        };
    }
}
