namespace AILA.Application.Features.ExpertEvaluations
{
    /// <summary>
    /// Mã lỗi nghiệp vụ của luồng Nhờ chuyên gia đánh giá.
    /// Controller dựa vào các mã này để ánh xạ sang HTTP status code.
    /// </summary>
    public static class ExpertEvaluationErrors
    {
        /// <summary>Lượt thực hành không tồn tại hoặc không thuộc học viên đang đăng nhập (AF-04).</summary>
        public const string PracticeAttemptNotFound = "PRACTICE_ATTEMPT_NOT_FOUND";

        /// <summary>Lượt thực hành chưa có kết quả chấm của AI (AF-01).</summary>
        public const string AiEvaluationUnavailable = "AI_EVALUATION_UNAVAILABLE";

        /// <summary>Lượt thực hành này đã có yêu cầu đánh giá (AF-02, BR-02).</summary>
        public const string EvaluationAlreadyRequested = "EVALUATION_ALREADY_REQUESTED";

        /// <summary>Học viên đã dùng hết lượt nhờ chuyên gia đánh giá (AF-03).</summary>
        public const string QuotaExhausted = "QUOTA_EXHAUSTED";

        /// <summary>Không tìm được chuyên gia tác giả khóa học đang hoạt động để giao việc (AC-29.7).</summary>
        public const string ExpertUnavailable = "EXPERT_UNAVAILABLE";

        /// <summary>Yêu cầu không tồn tại hoặc không thuộc phạm vi của người đang đăng nhập.</summary>
        public const string RequestNotFound = "EVALUATION_REQUEST_NOT_FOUND";

        /// <summary>Yêu cầu đã có kết quả, không được chấm lại (BR-03).</summary>
        public const string AlreadyEvaluated = "ALREADY_EVALUATED";

        /// <summary>Yêu cầu không ở trạng thái cho phép chấm (AC-64.6).</summary>
        public const string InvalidState = "INVALID_STATE";
    }
}
