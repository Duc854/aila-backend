using AILA.Domain.Common;
using AILA.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Domain.Entities
{
    public class Course : BaseEntity
    {
        public string Name { get; private set; }
        public string? Description { get; private set; }
        public Guid CategoryId { get; private set; }
        public Guid ExpertId { get; private set; }
        public string? ThumbnailUrl { get; private set; }
        public KnowledgeLevel Level { get; private set; }
        public decimal DurationHours { get; private set; }
        public bool IsPublished { get; private set; }
        public bool IsPublicationLocked { get; private set; }

        // Navigation Properties
        public virtual Category Category { get; private set; }
        public virtual Expert Expert { get; private set; }

        // Quản lý quan hệ Many-to-Many với Tag (Sử dụng Backing Field bảo vệ Collection)
        private readonly List<Tag> _courseTags = new();
        public virtual IReadOnlyCollection<Tag> CourseTags => _courseTags.AsReadOnly();
        // --- CẤU HÌNH QUAN HỆ 1-NHIỀU VỚI MODULE
        private readonly List<Module> _modules = new();
        public virtual IReadOnlyCollection<Module> Modules => _modules.AsReadOnly();
        //--- CẤU HÌNH QUAN HỆ 1-NHIỀU VỚI Request Review
        private readonly List<CourseReviewRequest> _reviewRequests = new();

        public virtual IReadOnlyCollection<CourseReviewRequest> ReviewRequests
            => _reviewRequests.AsReadOnly();

        // Constructor phục vụ EF Core
        private Course() { }

        // Constructor chuẩn DDD khi Expert khởi tạo một Khóa học nháp
        public Course(string name, Guid categoryId, Guid expertId, KnowledgeLevel level, string? description = null, string? thumbnailUrl = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Tên khóa học không được để trống.", nameof(name));

            if (categoryId == Guid.Empty) throw new ArgumentException("Mã danh mục không hợp lệ.");
            if (expertId == Guid.Empty) throw new ArgumentException("Mã chuyên gia không hợp lệ.");

            Id = Guid.NewGuid();
            Name = name.Trim();
            CategoryId = categoryId;
            ExpertId = expertId;
            Level = level;
            Description = description?.Trim();
            ThumbnailUrl = thumbnailUrl;
            DurationHours = 0.00m; // Ban đầu tạo mới thời lượng bằng 0
            IsPublished = false;
            IsPublicationLocked = false;// Mặc định tạo xong ở dạng Bản nháp (Draft)
        }

        // --- CÁC HÀNH VI NGHIỆP VỤ (METHODS) ---

        /// <summary>
        /// Chỉnh sửa thông tin cơ bản của khóa học
        /// </summary>
        public void UpdateInfo(string name, Guid categoryId, KnowledgeLevel level, string? description, string? thumbnailUrl)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Tên khóa học không được để trống.", nameof(name));

            if (categoryId == Guid.Empty) throw new ArgumentException("Mã danh mục không hợp lệ.");

            Name = name.Trim();
            CategoryId = categoryId;
            Level = level;
            Description = description?.Trim();
            ThumbnailUrl = thumbnailUrl;

            UpdateTimestamp();
        }

        /// <summary>
        /// Gắn hoặc cập nhật danh sách Tag cho khóa học
        /// </summary>
        public void AssignTags(List<Tag> tags)
        {
            if (tags == null) throw new ArgumentNullException(nameof(tags));

            _courseTags.Clear();
            _courseTags.AddRange(tags);
            UpdateTimestamp();
        }

        /// <summary>
        /// Cập nhật tổng thời lượng khóa học (thường gọi khi thêm/sửa/xóa Lesson)
        /// </summary>
        public void UpdateDuration(decimal hours)
        {
            if (hours < 0) throw new ArgumentException("Thời lượng không được nhỏ hơn 0.");

            DurationHours = hours;
            UpdateTimestamp();
        }

        /// <summary>
        /// Xuất bản khóa học để học viên có thể nhìn thấy và đăng ký học
        /// </summary>
        public void Publish()
        {
            if (IsPublished) return;

            if (IsPublicationLocked)
                throw new InvalidOperationException(
                    "Khóa học này cần được admin phê duyệt lại để publish do đã bị tố cáo");

            if (!_modules.Any())
                throw new InvalidOperationException(
                    "Khóa học phải có ít nhất một học phần trước khi xuất bản.");
            foreach (var module in _modules)
            {
                module.ValidateBeforeCoursePublish();
            }
            IsPublished = true;
            UpdateTimestamp();
        }

        /// <summary>
        /// Hạ khóa học xuống tạm thời ẩn đi
        /// </summary>
        public void Unpublish()
        {
            if (!IsPublished) return;

            IsPublished = false;
            UpdateTimestamp();
        }

        public void AddModule(Module module)
        {
            ArgumentNullException.ThrowIfNull(module);

            if (_modules.Any(m => m.Id == module.Id))
                throw new InvalidOperationException("Học phần đã tồn tại trong khóa học.");

            _modules.Add(module);

            UpdateTimestamp();
        }

        public void RemoveModule(Guid moduleId)
        {
            var module = _modules.FirstOrDefault(m => m.Id == moduleId)
                ?? throw new ArgumentException("Không tìm thấy học phần.", nameof(moduleId));

            _modules.Remove(module);

            UpdateTimestamp();
        }

        public void LockVisibility()
        {
            IsPublished = false;
            IsPublicationLocked = true;

            UpdateTimestamp();
        }

        public void RestorePublication()
        {
            if (!IsPublicationLocked)
                throw new InvalidOperationException("Khóa học không bị khóa.");
            if (IsPublished)
                return;

            IsPublicationLocked = false;
            IsPublished = true;

            UpdateTimestamp();
        }
    }
}
