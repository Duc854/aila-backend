using AILA.Domain.Entities;

namespace AILA.Application.Common.Interfaces.Repositories
{
    public interface IQuizRepository : IGenericRepository<QuizMaterial>
    {
        /// <summary>
        /// Lấy bài kiểm tra (QuizMaterial) kèm câu hỏi + đáp án, đồng thời xác thực
        /// bài kiểm tra này thuộc đúng khóa học (Material -> Module -> Course).
        /// </summary>
        Task<QuizMaterial?> GetQuizForLearningAsync(Guid courseId, Guid materialId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy lượt làm bài đang dở dang (InProgress) của một đăng ký học cho một bài kiểm tra.
        /// </summary>
        Task<QuizAttempt?> GetInProgressAttemptAsync(Guid enrollmentId, Guid quizMaterialId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy một lượt làm bài kèm các câu trả lời (dạng tracking để phục vụ chấm điểm/cập nhật).
        /// </summary>
        Task<QuizAttempt?> GetAttemptWithAnswersAsync(Guid attemptId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy lượt làm bài đã NỘP mới nhất của một đăng ký học cho một bài kiểm tra (read-only, kèm câu trả lời).
        /// Dùng cho màn xem kết quả (UC-27): mặc định hiển thị lần làm mới nhất (BR-02).
        /// </summary>
        Task<QuizAttempt?> GetLatestSubmittedAttemptAsync(Guid enrollmentId, Guid quizMaterialId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy toàn bộ lượt làm bài đã NỘP của một Learner (mọi khóa học), read-only,
        /// kèm QuizMaterial.Material (tên quiz) và Enrollment.Course (tên khóa học).
        /// Phục vụ khối "lịch sử quiz" ở Learning Profile (UC-30, AC-4). Lọc theo learnerId (BR-01).
        /// </summary>
        Task<List<QuizAttempt>> GetSubmittedAttemptsByLearnerAsync(Guid learnerId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Trang lịch sử quiz đã NỘP của một Learner (kèm QuizMaterial.Material + Enrollment.Course),
        /// mới nhất trước. Phục vụ màn "Xem tất cả lịch sử quiz" (UC-30). Lọc theo learnerId (BR-01).
        /// </summary>
        Task<(IEnumerable<QuizAttempt> Items, int TotalCount)> GetPagedSubmittedAttemptsByLearnerAsync(
            Guid learnerId, int pageIndex, int pageSize, CancellationToken cancellationToken = default);

        Task AddAttemptAsync(QuizAttempt attempt, CancellationToken cancellationToken = default);

        /// <summary>
        /// Thêm một câu trả lời mới vào DbSet để EF đánh dấu Added (INSERT).
        /// Cần gọi tường minh vì QuizAnswer tự sinh Id trong constructor (client-generated key),
        /// nếu chỉ thêm qua navigation của aggregate cha đang track thì EF hiểu nhầm là Modified.
        /// </summary>
        Task AddAnswerAsync(QuizAnswer answer, CancellationToken cancellationToken = default);
    }
}
