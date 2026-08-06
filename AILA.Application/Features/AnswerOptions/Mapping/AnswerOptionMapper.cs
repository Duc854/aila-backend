using AILA.Application.Features.AnswerOptions.Dtos;
using AILA.Domain.Entities;

namespace AILA.Application.Features.AnswerOptions.Mapping;

internal static class AnswerOptionMapper
{
    internal static AnswerOptionDto MapToDto(
        AnswerOption entity)
    {
        return new AnswerOptionDto
        {
            Id = entity.Id,
            QuestionId = entity.QuestionId,
            Content = entity.Content,
            IsCorrect = entity.IsCorrect,
            OrderIndex = entity.OrderIndex
        };
    }
}
