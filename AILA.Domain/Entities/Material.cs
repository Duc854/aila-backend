using AILA.Domain.Common;
using AILA.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Domain.Entities
{
    public class Material : BaseEntity
    {
        public Guid ModuleId { get; private set; }
        public string Title { get; private set; }
        public MaterialType MaterialType { get; private set; }
        public int OrderIndex { get; private set; }

        // Navigation Property
        public virtual Module Module { get; private set; }

        public virtual DocumentMaterial? DocumentDetails { get; private set; }
        public virtual VideoMaterial? VideoDetails { get; private set; }

        // Constructor phục vụ EF Core
        private Material() { }

        // Private Constructor để ép buộc khởi tạo qua các Factory Method tường minh
        private Material(Guid moduleId, string title, MaterialType materialType, int orderIndex)
        {
            if (moduleId == Guid.Empty)
                throw new ArgumentException("ModuleId không hợp lệ.", nameof(moduleId));

            if (string.IsNullOrWhiteSpace(title) || title.Length < 5 || title.Length > 255)
                throw new ArgumentException("Tiêu đề học liệu phải từ 5 đến 255 ký tự.", nameof(title));

            if (orderIndex < 0 || orderIndex > 9999)
                throw new ArgumentException("Vị trí sắp xếp (OrderIndex) phải nằm trong khoảng từ 0 đến 9999.", nameof(orderIndex));

            Id = Guid.NewGuid();
            ModuleId = moduleId;
            Title = title.Trim();
            MaterialType = materialType;
            OrderIndex = orderIndex;
        }

        // --- FACTORY METHODS (Tạo tường minh theo từng loại nội dung) ---

        public static Material CreateVideo(Guid moduleId, string title, int orderIndex)
        {
            return new Material(moduleId, title, MaterialType.Video, orderIndex);
        }

        public static Material CreateDocument(Guid moduleId, string title, int orderIndex)
        {
            return new Material(moduleId, title, MaterialType.Document, orderIndex);
        }

        public static Material CreateQuiz(Guid moduleId, string title, int orderIndex)
        {
            return new Material(moduleId, title, MaterialType.Quiz, orderIndex);
        }

        public static Material CreateAiPractice(Guid moduleId, string title, int orderIndex)
        {
            return new Material(moduleId, title, MaterialType.AiPractice, orderIndex);
        }

        // --- CÁC HÀNH VI NGHIỆP VỤ (METHODS) ---

        /// <summary>
        /// Chỉnh sửa tiêu đề hiển thị của học liệu
        /// </summary>
        public void UpdateTitle(string newTitle)
        {
            if (string.IsNullOrWhiteSpace(newTitle) || newTitle.Length < 5 || newTitle.Length > 255)
                throw new ArgumentException("Tiêu đề học liệu phải từ 5 đến 255 ký tự.", nameof(newTitle));

            Title = newTitle.Trim();
            UpdateTimestamp();
        }

        /// <summary>
        /// Thay đổi thứ tự xuất hiện của học liệu trong chương
        /// </summary>
        public void ChangeOrder(int newOrderIndex)
        {
            if (newOrderIndex < 0 || newOrderIndex > 9999)
                throw new ArgumentException("Vị trí sắp xếp phải nằm trong khoảng từ 0 đến 9999.", nameof(newOrderIndex));

            if (OrderIndex == newOrderIndex) return;

            OrderIndex = newOrderIndex;
            UpdateTimestamp();
        }
    }
}
