using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Domain.Entities
{
    public class LearningProgress
    {
        public Guid EnrollmentId { get; private set; }
        public Guid MaterialId { get; private set; }
        public bool IsCompleted { get; private set; }
        public DateTime? CompletedAt { get; private set; }
        public DateTime LastAccessedAt { get; private set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual Enrollment Enrollment { get; private set; }
        public virtual Material Material { get; private set; }

        // Constructor phục vụ EF Core
        private LearningProgress() { }

        // Constructor chuẩn DDD khi học viên click mở một bài học lần đầu tiên
        public LearningProgress(Guid enrollmentId, Guid materialId)
        {
            if (enrollmentId == Guid.Empty) throw new ArgumentException("Mã ghi danh không hợp lệ.");
            if (materialId == Guid.Empty) throw new ArgumentException("Mã học liệu không hợp lệ.");

            EnrollmentId = enrollmentId;
            MaterialId = materialId;
            IsCompleted = false;
            CompletedAt = null;
        }

        // --- CÁC HÀNH VI NGHIỆP VỤ ---

        /// <summary>
        /// Đánh dấu học viên đã học xong bài này (Ví dụ: xem hết video hoặc đọc xong document)
        /// </summary>
        public void Complete()
        {
            if (IsCompleted) return;

            IsCompleted = true;
            CompletedAt = DateTime.UtcNow;
            LastAccessedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Cập nhật thời gian tương tác cuối cùng với bài học
        /// </summary>
        public void TrackAccess()
        {
            LastAccessedAt = DateTime.UtcNow;
        }
    }
}
