using AILA.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Domain.Entities
{
    public class StepGuidance
    {
        public Guid Id { get; private set; }

        public Guid AIPracticeMaterialId { get; private set; }

        public int OrderIndex { get; private set; }

        public string Content { get; private set; }

        // Navigation
        public virtual AIPracticeMaterial AIPracticeMaterial { get; private set; } = null!;

        private StepGuidance() { }

        public StepGuidance(
            Guid aiPracticeMaterialId,
            int orderIndex,
            string content)
        {
            if (aiPracticeMaterialId == Guid.Empty)
                throw new ArgumentException("AI Practice Material không hợp lệ.");

            if (orderIndex < 1)
                throw new ArgumentException("Thứ tự bước phải lớn hơn 0.");

            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Nội dung hướng dẫn không được để trống.");

            Id = Guid.NewGuid();
            AIPracticeMaterialId = aiPracticeMaterialId;
            OrderIndex = orderIndex;
            Content = content.Trim();
        }

        public void Update(
            int orderIndex,
            string content)
        {
            if (orderIndex < 1)
                throw new ArgumentException("Thứ tự bước phải lớn hơn 0.");

            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Nội dung hướng dẫn không được để trống.");

            OrderIndex = orderIndex;
            Content = content.Trim();
        }
    }
}
