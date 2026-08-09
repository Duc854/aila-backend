using AILA.Domain.Common;
using System;

namespace AILA.Domain.Entities
{
    /// <summary>
    /// Kết quả chấm của chuyên gia, lưu tách biệt với đánh giá của AI (BR-01).
    /// Bất biến sau khi tạo: không có hành vi sửa/xóa kết quả (BR-03).
    /// </summary>
    public class ExpertEvaluation : BaseEntity
    {
        public Guid EvaluationRequestId { get; private set; }

        public decimal OverallScore { get; private set; }

        public string Feedback { get; private set; } = string.Empty;

        /// <summary>
        /// Khuyến nghị cải thiện, không bắt buộc (UC-64).
        /// Cột trong DB là NOT NULL nên khi chuyên gia bỏ trống sẽ lưu chuỗi rỗng.
        /// </summary>
        public string Recommendation { get; private set; } = string.Empty;

        public DateTime EvaluatedAt { get; private set; }

        public virtual ExpertEvaluationRequest EvaluationRequest { get; private set; } = null!;

        private ExpertEvaluation() { }

        public ExpertEvaluation(
            Guid evaluationRequestId,
            decimal overallScore,
            string feedback,
            string? recommendation = null)
        {
            if (evaluationRequestId == Guid.Empty)
                throw new ArgumentException("Mã yêu cầu đánh giá không hợp lệ.", nameof(evaluationRequestId));

            if (overallScore < 0)
                throw new ArgumentException("Điểm đánh giá không được nhỏ hơn 0.", nameof(overallScore));

            if (string.IsNullOrWhiteSpace(feedback))
                throw new ArgumentException("Phản hồi của chuyên gia không được để trống.", nameof(feedback));

            Id = Guid.NewGuid();
            EvaluationRequestId = evaluationRequestId;
            OverallScore = overallScore;
            Feedback = feedback.Trim();
            Recommendation = string.IsNullOrWhiteSpace(recommendation)
                ? string.Empty
                : recommendation.Trim();

            EvaluatedAt = DateTime.UtcNow;
        }
    }
}
