namespace AILA.Application.Features.Quizzes.Dtos
{
    /// <summary>
    /// Kết quả sau khi nộp bài kiểm tra (đóng băng, bất biến sau khi commit).
    /// </summary>
    public class QuizResultDto
    {
        public Guid AttemptId { get; set; }

        /// <summary>Điểm chuẩn hóa 0.00 - 100.00.</summary>
        public decimal Score { get; set; }

        public bool IsPassed { get; set; }
        public decimal PassingScore { get; set; }

        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }

        public DateTime? SubmittedAt { get; set; }

        /// <summary>True nếu bài được tự động nộp do hết giờ (AF-01).</summary>
        public bool WasAutoSubmitted { get; set; }

        /// <summary>Tiến độ khóa học sau khi cập nhật (%).</summary>
        public decimal ProgressPct { get; set; }
    }
}
