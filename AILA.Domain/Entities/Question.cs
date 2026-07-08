using AILA.Domain.Common;
using AILA.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Domain.Entities
{
    public class Question : BaseEntity
    {
        private readonly List<AnswerOption> _answerOptions = new();

        public Guid QuizMaterialId { get; private set; }

        public string Content { get; private set; } = string.Empty;

        public QuestionType QuestionType { get; private set; }

        public int OrderIndex { get; private set; }

        // Navigation Properties
        public virtual QuizMaterial QuizMaterial { get; private set; } = null!;

        public virtual IReadOnlyCollection<AnswerOption> AnswerOptions => _answerOptions.AsReadOnly();

        private Question() { }

        public Question(
            Guid quizMaterialId,
            string content,
            QuestionType questionType,
            int orderIndex)
        {
            if (quizMaterialId == Guid.Empty)
                throw new ArgumentException("Mã bài kiểm tra không hợp lệ.", nameof(quizMaterialId));

            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Nội dung câu hỏi không được để trống.", nameof(content));

            if (orderIndex < 1)
                throw new ArgumentOutOfRangeException(nameof(orderIndex), "Thứ tự hiển thị phải lớn hơn 0.");

            Id = Guid.NewGuid();
            QuizMaterialId = quizMaterialId;
            Content = content.Trim();
            QuestionType = questionType;
            OrderIndex = orderIndex;
        }

        public void Update(
            string content,
            QuestionType questionType,
            int orderIndex)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Nội dung câu hỏi không được để trống.", nameof(content));

            if (orderIndex < 1)
                throw new ArgumentOutOfRangeException(nameof(orderIndex), "Thứ tự hiển thị phải lớn hơn 0.");

            Content = content.Trim();
            QuestionType = questionType;
            OrderIndex = orderIndex;

            UpdateTimestamp();
        }

        public void AddAnswerOption(AnswerOption option)
        {
            ArgumentNullException.ThrowIfNull(option);

            if (_answerOptions.Any(x => x.Id == option.Id))
                throw new InvalidOperationException("Đáp án đã tồn tại trong câu hỏi.");

            _answerOptions.Add(option);

            UpdateTimestamp();
        }

        public void RemoveAnswerOption(Guid answerOptionId)
        {
            var option = _answerOptions.FirstOrDefault(x => x.Id == answerOptionId)
                ?? throw new ArgumentException("Không tìm thấy đáp án.", nameof(answerOptionId));

            _answerOptions.Remove(option);

            UpdateTimestamp();
        }

        public void ValidateAnswerOptions()
        {
            var correctCount = _answerOptions.Count(x => x.IsCorrect);

            switch (QuestionType)
            {
                case QuestionType.SingleChoice:
                    if (correctCount != 1)
                        throw new InvalidOperationException(
                            "Câu hỏi một đáp án chỉ được phép có đúng một đáp án đúng.");
                    break;

                case QuestionType.MultipleChoice:
                    if (correctCount == 0)
                        throw new InvalidOperationException(
                            "Câu hỏi nhiều đáp án phải có ít nhất một đáp án đúng.");
                    break;
            }
        }
    }
}
