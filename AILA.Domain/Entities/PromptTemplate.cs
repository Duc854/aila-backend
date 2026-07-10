using AILA.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Domain.Entities
{
    public class PromptTemplate
    {
        public Guid Id { get; private set; }

        public Guid AIPracticeMaterialId { get; private set; }

        public string Title { get; private set; }

        public string Content { get; private set; }

        // Navigation
        public virtual AIPracticeMaterial AIPracticeMaterial { get; private set; } = null!;

        private PromptTemplate() { }

        public PromptTemplate(
            Guid aiPracticeMaterialId,
            string title,
            string content)
        {
            if (aiPracticeMaterialId == Guid.Empty)
                throw new ArgumentException("AI Practice Material không hợp lệ.");

            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Tiêu đề prompt mẫu không được để trống.");

            if (title.Length > 100)
                throw new ArgumentException("Tiêu đề prompt mẫu không được vượt quá 100 ký tự.");

            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Nội dung prompt mẫu không được để trống.");

            Id = Guid.NewGuid();
            AIPracticeMaterialId = aiPracticeMaterialId;
            Title = title.Trim();
            Content = content.Trim();
        }

        public void Update(
            string title,
            string content)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Tiêu đề prompt mẫu không được để trống.");

            if (title.Length > 100)
                throw new ArgumentException("Tiêu đề prompt mẫu không được vượt quá 100 ký tự.");

            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Nội dung prompt mẫu không được để trống.");

            Title = title.Trim();
            Content = content.Trim();
        }
    }
}
