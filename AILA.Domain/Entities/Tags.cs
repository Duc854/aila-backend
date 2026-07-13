using AILA.Domain.Common;
using AILA.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Domain.Entities
{
    public class Tag : BaseEntity
    {
        public string Name { get; private set; }
        public string Code { get; private set; }
        public bool IsPublished { get; private set; }
        public Guid? CreatedById { get; private set; } // Null nếu do Hệ thống/Admin tạo, có giá trị nếu do Expert tạo

        public virtual TagPublishRequest? PublishRequest { get; private set; }

        // Constructor phục vụ EF Core
        private Tag() { }

        // Constructor dành cho Admin/Hệ thống tạo (Mặc định duyệt luôn hoặc tùy chọn)
        private Tag(string name, string code, bool isPublished, Guid? createdById)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Tên thẻ không được để trống.");
            if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Mã thẻ không được để trống.");

            Id = Guid.NewGuid();
            Name = name.Trim();
            Code = code.Trim().ToLower().Replace(" ", "-"); // Chuẩn hóa dạng slug nếu cần
            IsPublished = isPublished;
            CreatedById = createdById;
        }

        /// <summary>
        /// Expert tự tạo tự do trong lúc biên soạn Khóa học. 
        /// Ép trạng thái IsPublished = false, chờ Admin duyệt mới đưa vào danh sách chung.
        /// </summary>
        public static Tag CreateByExpert(string name, string code, Guid expertId)
        {
            if (expertId == Guid.Empty) throw new ArgumentException("Mã chuyên gia không hợp lệ.", nameof(expertId));

            return new Tag(name, code, isPublished: false, createdById: expertId);
        }

        /// <summary>
        /// Admin hoặc Hệ thống tạo Tag gốc. Mặc định sẽ được hiển thị công khai và dùng để recommend.
        /// </summary>
        public static Tag CreateByAdmin(string name, string code)
        {
            return new Tag(name, code, isPublished: true, createdById: null);
        }

        public void CreatePublishRequest(string? note)
        {
            if (IsPublished)
                throw new InvalidOperationException("Tag đã được xuất bản.");

            if (PublishRequest != null &&
                PublishRequest.Status == TagPublishRequestStatus.Pending)
                throw new InvalidOperationException("Đã tồn tại yêu cầu chờ duyệt.");

            PublishRequest = new TagPublishRequest(Id, note);

            UpdateTimestamp();
        }

        /// <summary>
        /// Admin duyệt các Tag do Expert đề xuất
        /// </summary>
        public void Publish()
        {
            if (IsPublished) return; // Đã duyệt rồi thì bỏ qua

            IsPublished = true;
            UpdateTimestamp();
        }

        public void UpdateSystemTag(string name, string code)
        {
            if (CreatedById != null)
                throw new InvalidOperationException(
                    "Chỉ System Tag mới được cập nhật.");

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException(
                    "Tên tag không được để trống.");

            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException(
                    "Code tag không được để trống.");

            Name = name.Trim();
            Code = code.Trim().ToLower().Replace(" ", "-");

            UpdateTimestamp();
        }
    }
}
