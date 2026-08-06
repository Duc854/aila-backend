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

        // Navigation Properties
        public virtual Module Module { get; private set; } = null!;

        public virtual VideoMaterial? VideoDetails { get; private set; }

        public virtual DocumentMaterial? DocumentDetails { get; private set; }

        public virtual QuizMaterial? QuizDetails { get; private set; }

        public virtual AIPracticeMaterial? AIPracticeDetails { get; private set; }

        private readonly List<ContentReport> _contentReports = new();

        public virtual IReadOnlyCollection<ContentReport> ContentReports
            => _contentReports.AsReadOnly();

        // Constructor phục vụ EF Core
        private Material() { }

        // Constructor nội bộ, chỉ được gọi thông qua Factory Method
        private Material(
            Guid moduleId,
            string title,
            MaterialType materialType,
            int orderIndex)
        {
            if (moduleId == Guid.Empty)
                throw new ArgumentException("Mã học phần không hợp lệ.", nameof(moduleId));

            if (string.IsNullOrWhiteSpace(title) || title.Length < 5 || title.Length > 255)
                throw new ArgumentException("Tiêu đề học liệu phải từ 5 đến 255 ký tự.", nameof(title));

            if (orderIndex < 1)
                throw new ArgumentException("Vị trí hiển thị phải lớn hơn 0.", nameof(orderIndex));

            Id = Guid.NewGuid();
            ModuleId = moduleId;
            Title = title.Trim();
            MaterialType = materialType;
            OrderIndex = orderIndex;
        }


        public static Material CreateVideo(Guid moduleId, string title, int orderIndex)
            => new(moduleId, title, MaterialType.Video, orderIndex);

        public static Material CreateDocument(Guid moduleId, string title, int orderIndex)
            => new(moduleId, title, MaterialType.Document, orderIndex);

        public static Material CreateQuiz(Guid moduleId, string title, int orderIndex)
            => new(moduleId, title, MaterialType.Quiz, orderIndex);

        public static Material CreateAiPractice(Guid moduleId, string title, int orderIndex)
            => new(moduleId, title, MaterialType.AiPractice, orderIndex);



        public void UpdateTitle(string newTitle)
        {
            if (string.IsNullOrWhiteSpace(newTitle) || newTitle.Length < 5 || newTitle.Length > 255)
                throw new ArgumentException("Tiêu đề học liệu phải từ 5 đến 255 ký tự.", nameof(newTitle));

            Title = newTitle.Trim();

            UpdateTimestamp();
        }

        public void ChangeOrder(int newOrderIndex)
        {
            if (newOrderIndex < 1)
                throw new ArgumentException("Vị trí hiển thị phải lớn hơn 0.", nameof(newOrderIndex));

            if (OrderIndex == newOrderIndex)
                return;

            OrderIndex = newOrderIndex;

            UpdateTimestamp();
        }

    }
}
