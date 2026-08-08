using AILA.Domain.Entities;
using AILA.Domain.Enums;

namespace AILA.Application.Common.Notifications
{
    /// <summary>
    /// Nơi tập trung nội dung của mọi thông báo trong hệ thống.
    ///
    /// <para><b>Quy ước cho người thêm mới:</b> ai làm nghiệp vụ nào thì thêm một static method
    /// vào đây cho nghiệp vụ đó, đặt tên theo sự kiện (không phải theo người nhận), rồi gọi
    /// trong handler tương ứng. Không tự <c>new Notification(...)</c> rải rác trong handler —
    /// gom về đây để nội dung, giọng văn và <see cref="NotificationType"/> còn nhất quán.</para>
    ///
    /// <para><b>Cách dùng trong handler:</b> chỉ cần <c>AddAsync</c> trước lần
    /// <c>SaveChangesAsync</c> đã có sẵn của handler. Vì dùng chung một DbContext nên dữ liệu
    /// nghiệp vụ và thông báo tự nằm chung một transaction, không cần
    /// <c>BeginTransactionAsync</c> thủ công:</para>
    /// <code>
    /// await _uow.Notifications.AddAsync(NotificationTemplates.XxxHappened(...));
    /// await _uow.SaveChangesAsync(ct);   // save vốn đã có
    /// </code>
    ///
    /// <para><b>Lưu ý:</b> <c>RedirectUrl</c> phải khớp route của frontend (repo aila-frontend,
    /// <c>src/router/AppRouter.jsx</c>), không phải route của API. Đổi route bên FE thì phải sửa
    /// lại ở đây, nếu không thông báo sẽ dẫn người dùng tới trang 404.</para>
    /// </summary>
    public static class NotificationTemplates
    {
        /// <summary>Lệch giờ Việt Nam so với UTC, dùng để hiển thị mốc thời gian trong nội dung thông báo.</summary>
        private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

        // ----------------------------------------------------------------------------------
        // Tài khoản & bảo mật
        // ----------------------------------------------------------------------------------

        /// <summary>
        /// Ghi nhận mốc thời gian tài khoản được đặt lại mật khẩu thành công.
        /// Áp dụng cho mọi vai trò (Learner/Expert/Admin) vì luồng reset là luồng dùng chung.
        /// </summary>
        /// <param name="userId">Chủ tài khoản vừa đổi mật khẩu.</param>
        /// <param name="occurredAtUtc">Thời điểm đổi mật khẩu, theo UTC.</param>
        public static Notification PasswordResetSucceeded(Guid userId, DateTime occurredAtUtc)
            => new(
                userId,
                "Mật khẩu đã được đặt lại",
                $"Mật khẩu tài khoản của bạn đã được đặt lại thành công vào lúc {FormatVietnamTime(occurredAtUtc)}. "
                + "Nếu không phải bạn thực hiện, hãy đổi mật khẩu và liên hệ quản trị viên ngay.",
                NotificationType.ResetPasswordSuccessful);

        // ----------------------------------------------------------------------------------
        // Nhờ chuyên gia đánh giá
        // ----------------------------------------------------------------------------------

        /// <summary>
        /// Báo cho chuyên gia tác giả khóa học rằng có một yêu cầu đánh giá mới được giao.
        /// </summary>
        /// <param name="expertId">Chuyên gia được giao yêu cầu.</param>
        /// <param name="requestId">Mã yêu cầu đánh giá vừa tạo.</param>
        /// <param name="courseName">Tên khóa học chứa lượt thực hành, để chuyên gia định vị nhanh.</param>
        public static Notification ExpertEvaluationRequested(
            Guid expertId,
            Guid requestId,
            string courseName)
            => new(
                expertId,
                "Bạn có yêu cầu đánh giá mới",
                $"Một học viên vừa nhờ bạn đánh giá bài thực hành thuộc khóa học \"{courseName}\". "
                + "Vào phần Yêu cầu đánh giá để xem chi tiết và chấm điểm.",
                NotificationType.NewEvaluationRequest,
                $"/expert/evaluation-requests/{requestId}");

        /// <summary>
        /// Báo cho học viên rằng chuyên gia đã chấm xong yêu cầu của mình.
        /// </summary>
        /// <param name="learnerId">Học viên đã gửi yêu cầu.</param>
        /// <param name="requestId">Mã yêu cầu đánh giá đã hoàn tất.</param>
        /// <param name="overallScore">Điểm tổng chuyên gia chấm.</param>
        public static Notification ExpertEvaluationCompleted(
            Guid learnerId,
            Guid requestId,
            decimal overallScore)
            => new(
                learnerId,
                "Chuyên gia đã đánh giá bài thực hành của bạn",
                $"Chuyên gia đã hoàn tất đánh giá với điểm tổng {overallScore:0.##}. "
                + "Xem nhận xét và gợi ý cải thiện chi tiết trong kết quả đánh giá.",
                NotificationType.ReceiveExpertEvaluation,
                $"/learner/expert-evaluations/{requestId}");

        /// <summary>Đổi mốc UTC sang giờ Việt Nam để người đọc không phải tự quy đổi.</summary>
        private static string FormatVietnamTime(DateTime utc)
            => $"{utc.Add(VietnamOffset):HH:mm 'ngày' dd/MM/yyyy} (giờ Việt Nam)";
    }
}
