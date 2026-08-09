namespace Shared.Models
{
    /// <summary>
    /// Cấu hình luồng Nhờ chuyên gia đánh giá (UC-29/30/63/64).
    /// Khoảng điểm, giới hạn độ dài và kích thước trang đều nằm ở đây (Options pattern)
    /// thay vì hardcode trong handler.
    /// </summary>
    public class ExpertEvaluationSettings
    {
        /// <summary>Điểm tổng tối thiểu chuyên gia được phép chấm.</summary>
        public decimal MinScore { get; set; } = 0m;

        /// <summary>Điểm tổng tối đa chuyên gia được phép chấm.</summary>
        public decimal MaxScore { get; set; } = 10m;

        /// <summary>Số chữ số thập phân tối đa của điểm tổng (cột DB có precision 5,2).</summary>
        public int ScoreDecimals { get; set; } = 2;

        /// <summary>Độ dài tối đa của phản hồi bắt buộc (khớp cột DB 2000 ký tự).</summary>
        public int FeedbackMaxLength { get; set; } = 2000;

        /// <summary>Độ dài tối đa của khuyến nghị tuỳ chọn (khớp cột DB 1000 ký tự).</summary>
        public int RecommendationMaxLength { get; set; } = 1000;

        /// <summary>Kích thước trang mặc định của danh sách hàng chờ chuyên gia (UC-63).</summary>
        public int DefaultPageSize { get; set; } = 20;

        /// <summary>Kích thước trang tối đa, chặn client yêu cầu trang quá lớn (E63-1).</summary>
        public int MaxPageSize { get; set; } = 100;
    }
}
