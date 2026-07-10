using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.QuizMaterials.Dtos.BulkCreateQuiz;

public sealed class BulkCreateQuizRequest
{
    public int TimeLimitMinutes { get; set; }

    public decimal PassingScore { get; set; }

    public bool ShowCorrectAnswersAfterSubmission { get; set; }

    public List<BulkQuestionDto> Questions { get; set; } = [];
}
