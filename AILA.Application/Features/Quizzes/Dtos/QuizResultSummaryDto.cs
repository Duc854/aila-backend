namespace AILA.Application.Features.Quizzes.Dtos
{
    /// <summary>
    /// Tóm tắt kết quả bài kiểm tra (UC-27): hiển thị lần làm MỚI NHẤT.
    /// Read-only, đọc đúng như dữ liệu đã đóng băng ở UC-26 (không tính lại điểm).
    /// </summary>
    public class QuizResultSummaryDto
    {
        /// <summary>False = chưa có lượt làm nào đã nộp → hiển thị empty state (AC-5).</summary>
        public bool HasResult { get; set; }

        public Guid? AttemptId { get; set; }
        public decimal? Score { get; set; }
        public bool? IsPassed { get; set; }
        public decimal PassingScore { get; set; }

        public int TotalQuestions { get; set; }
        public int? CorrectAnswers { get; set; }

        public DateTime? StartedAt { get; set; }

        /// <summary>Thời điểm hoàn thành (nộp bài).</summary>
        public DateTime? SubmittedAt { get; set; }

        /// <summary>Có được mở màn xem chi tiết đáp án hay không (theo cấu hình quiz, BR-01).</summary>
        public bool CanViewDetails { get; set; }
    }
}
