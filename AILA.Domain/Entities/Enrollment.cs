using AILA.Domain.Common;
using AILA.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Domain.Entities
{
    public class Enrollment : BaseEntity
    {
        public Guid LearnerId { get; private set; }
        public Guid CourseId { get; private set; }
        public EnrollmentStatus Status { get; private set; }
        public int TotalMaterials { get; private set; }
        public int CompletedMaterials { get; private set; }
        public decimal ProgressPct { get; private set; }
        public DateTime EnrolledAt { get; private set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; private set; }
        public DateTime? LastAccessedAt { get; private set; }

        // Navigation Properties
        public virtual Learner Learner { get; private set; }
        public virtual Course Course { get; private set; }

        // Constructor phục vụ EF Core
        private Enrollment() { }

        // Constructor chuẩn DDD khi Học viên bắt đầu bấm tham gia khóa học
        public Enrollment(Guid learnerId, Guid courseId, int totalMaterials)
        {
            if (learnerId == Guid.Empty) throw new ArgumentException("Mã học viên không hợp lệ.");
            if (courseId == Guid.Empty) throw new ArgumentException("Mã khóa học không hợp lệ.");
            if (totalMaterials < 0) throw new ArgumentException("Tổng số học liệu không được nhỏ hơn 0.");

            Id = Guid.NewGuid();
            LearnerId = learnerId;
            CourseId = courseId;
            TotalMaterials = totalMaterials;
            CompletedMaterials = 0;
            ProgressPct = 0.00m;
            Status = EnrollmentStatus.Active;
            EnrolledAt = DateTime.UtcNow;
        }

        // --- CÁC HÀNH VI NGHIỆP VỤ (METHODS) ---

        /// <summary>
        /// Cập nhật thời gian cuối cùng học viên tương tác với khóa học
        /// </summary>
        public void TrackAccess()
        {
            LastAccessedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Ghi nhận khi học viên hoàn thành thêm 1 học liệu (Video, Doc, Quiz...) trong khóa học
        /// </summary>
        public bool CompleteMaterial()
        {
            if (Status == EnrollmentStatus.Completed)
                return false;


            if (CompletedMaterials >= TotalMaterials)
                throw new InvalidOperationException(
                    "Số học liệu hoàn thành không thể vượt quá tổng số học liệu.");


            CompletedMaterials++;

            var wasCompleted = Status == EnrollmentStatus.Completed;

            CalculateProgress();

            TrackAccess();


            return !wasCompleted
                && Status == EnrollmentStatus.Completed;
        }

        /// <summary>
        /// Ghi nhận nếu học viên hủy bỏ trạng thái hoàn thành của 1 bài học (nếu có tính năng học lại/làm lại)
        /// </summary>
        public void UncompleteMaterial()
        {
            if (CompletedMaterials <= 0) return;

            CompletedMaterials--;

            // Nếu đang ở trạng thái Completed mà bị lùi bài, hạ trạng thái xuống Active
            if (Status == EnrollmentStatus.Completed)
            {
                Status = EnrollmentStatus.Active;
                CompletedAt = null;
            }

            CalculateProgress();
            TrackAccess();
        }

        /// <summary>
        /// Cập nhật lại tổng số học liệu khi khóa học được Expert thêm bài mới
        /// </summary>
        public void UpdateTotalMaterials(int newTotal)
        {
            if (newTotal < CompletedMaterials)
                throw new ArgumentException("Tổng số học liệu mới không được nhỏ hơn số học liệu học viên đã hoàn thành.");

            TotalMaterials = newTotal;
            CalculateProgress();
        }

        // --- HÀM TÍNH TOÁN NỘI BỘ (DOMAINS INVARIANTS) ---
        private void CalculateProgress()
        {
            if (TotalMaterials == 0)
            {
                ProgressPct = 0.00m;
                return;
            }

            // Tính % tiến độ làm tròn 2 chữ số thập phân
            ProgressPct = Math.Round(((decimal)CompletedMaterials / TotalMaterials) * 100, 2);

            // Quy tắc tự động chuyển trạng thái khi học viên học hết 100% số bài
            if (CompletedMaterials == TotalMaterials && Status != EnrollmentStatus.Completed)
            {
                Status = EnrollmentStatus.Completed;
                CompletedAt = DateTime.UtcNow;
            }
        }

        public bool IsCompleted()
        {
            return CompletedMaterials >= TotalMaterials;
        }
    }
}
