using AILA.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Domain.Entities
{
    public class QuizAnswer : BaseEntity
    {
        public Guid QuizAttemptId { get; private set; }

        public Guid QuestionId { get; private set; }

        public Guid? SelectedAnswerOptionId { get; private set; }

        public virtual QuizAttempt QuizAttempt { get; private set; } = null!;

        public virtual Question Question { get; private set; } = null!;

        public virtual AnswerOption? SelectedAnswerOption { get; private set; }


        public QuizAnswer(
            Guid quizAttemptId,
            Guid questionId,
            Guid? selectedAnswerOptionId)
        {
            if (quizAttemptId == Guid.Empty)
                throw new ArgumentException("Lượt làm bài không hợp lệ.");

            if (questionId == Guid.Empty)
                throw new ArgumentException("Câu hỏi không hợp lệ.");

            Id = Guid.NewGuid();
            QuizAttemptId = quizAttemptId;
            QuestionId = questionId;
            SelectedAnswerOptionId = selectedAnswerOptionId;
        }

        public void ChangeAnswer(Guid? selectedAnswerOptionId)
        {
            SelectedAnswerOptionId = selectedAnswerOptionId;
            UpdateTimestamp();
        }
    }
}
