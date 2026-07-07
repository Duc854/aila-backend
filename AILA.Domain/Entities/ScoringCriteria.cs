using AILA.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Domain.Entities
{
    public class ScoringCriteria
    {
        public Guid Id { get; private set; }

        public Guid AIPracticeMaterialId { get; private set; }

        public string Title { get; private set; }

        public string? Description { get; private set; }

        public decimal Weight { get; private set; }

        // Navigation
        public virtual AIPracticeMaterial AIPracticeMaterial { get; private set; } = null!;

        private ScoringCriteria() { }

        public ScoringCriteria(
            Guid aiPracticeMaterialId,
            string title,
            string? description,
            decimal weight)
        {
            if (aiPracticeMaterialId == Guid.Empty)
                throw new ArgumentException("AI Practice Material không hợp lệ.");

            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Tên tiêu chí không được để trống.");

            if (weight <= 0 || weight > 100)
                throw new ArgumentException("Trọng số phải nằm trong khoảng từ 0 đến 100.");

            Id = Guid.NewGuid();
            AIPracticeMaterialId = aiPracticeMaterialId;
            Title = title.Trim();
            Description = description?.Trim();
            Weight = weight;
        }

        public void Update(
            string title,
            string? description,
            decimal weight)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Tên tiêu chí không được để trống.");

            if (weight <= 0 || weight > 100)
                throw new ArgumentException("Trọng số phải nằm trong khoảng từ 0 đến 100.");

            Title = title.Trim();
            Description = description?.Trim();
            Weight = weight;
        }
    }
}
