using AILA.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Domain.Entities
{
    public class AnswerOption : BaseEntity
    {
        public Guid QuestionId { get; private set; }

        public string Content { get; private set; }

        public bool IsCorrect { get; private set; }

        public int OrderIndex { get; private set; }

        // Navigation Property
        public virtual Question Question { get; private set; } = null!;

        // Constructor phục vụ EF Core
        private AnswerOption() { }

        public AnswerOption(
            Guid questionId,
            string content,
            bool isCorrect,
            int orderIndex)
        {
            if (questionId == Guid.Empty)
                throw new ArgumentException("Mã câu hỏi không hợp lệ.", nameof(questionId));

            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Nội dung đáp án không được để trống.", nameof(content));

            if (orderIndex < 1)
                throw new ArgumentException("Thứ tự hiển thị phải lớn hơn 0.", nameof(orderIndex));

            Id = Guid.NewGuid();
            QuestionId = questionId;
            Content = content.Trim();
            IsCorrect = isCorrect;
            OrderIndex = orderIndex;
        }

        /// <summary>
        /// Cập nhật thông tin đáp án
        /// </summary>
        public void Update(
            string content,
            bool isCorrect,
            int orderIndex)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Nội dung đáp án không được để trống.", nameof(content));

            if (orderIndex < 1)
                throw new ArgumentException("Thứ tự hiển thị phải lớn hơn 0.", nameof(orderIndex));

            Content = content.Trim();
            IsCorrect = isCorrect;
            OrderIndex = orderIndex;

            UpdateTimestamp();
        }

        /// <summary>
        /// Thay đổi thứ tự hiển thị
        /// </summary>
        public void ChangeOrder(int newOrderIndex)
        {
            if (newOrderIndex < 1)
                throw new ArgumentException("Thứ tự hiển thị phải lớn hơn 0.", nameof(newOrderIndex));

            if (OrderIndex == newOrderIndex)
                return;

            OrderIndex = newOrderIndex;

            UpdateTimestamp();
        }

        /// <summary>
        /// Đánh dấu đáp án đúng hoặc sai
        /// </summary>
        public void SetCorrect(bool isCorrect)
        {
            if (IsCorrect == isCorrect)
                return;

            IsCorrect = isCorrect;

            UpdateTimestamp();
        }
    }
}
