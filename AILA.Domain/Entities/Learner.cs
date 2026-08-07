using AILA.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Domain.Entities
{
    public class Learner
    {
        public Guid UserId { get; private set; }
        public LearnerType? LearnerType { get; private set; }
        public KnowledgeLevel? KnowledgeLevel { get; private set; }
        public bool HasCompletedOnboarding { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        // Sử dụng Backing Field để bảo vệ Collection chuẩn DDD
        private readonly List<Tag> _learningGoals = new();

        // Chỉ expose ra ngoài dưới dạng ReadOnly, không cho phép dùng .Add() trực tiếp từ bên ngoài
        public virtual IReadOnlyCollection<Tag> LearningGoals => _learningGoals.AsReadOnly();

        private readonly List<LearnerTagScore> _tagScores = new();

        public IReadOnlyCollection<LearnerTagScore> TagScores
            => _tagScores.AsReadOnly();

        public virtual User User { get; private set; }

        private Learner() { }

        public Learner(Guid userId)
        {
            if (userId == Guid.Empty) throw new ArgumentException("Mã người dùng không hợp lệ.");
            UserId = userId;
            HasCompletedOnboarding = false;
        }

        // --- HÀNH VI NGHIỆP VỤ ---

        /// <summary>
        /// Hoàn thành khảo sát onboarding với danh sách Tag mục tiêu đã được duyệt
        /// </summary>
        public void CompleteOnboarding(LearnerType type, KnowledgeLevel level, List<Tag> selectedTags)
        {
            if (HasCompletedOnboarding)
                throw new InvalidOperationException(
                    "Học viên đã hoàn thành khảo sát ban đầu.");
            if (selectedTags == null || !selectedTags.Any())
                throw new ArgumentException("Học viên phải chọn ít nhất một mục tiêu học tập.");

            // Domain Validation: Chỉ cho phép chọn các Tag đã được Admin duyệt (IsPublished = true)
            if (selectedTags.Any(t => !t.IsPublished))
                throw new InvalidOperationException("Không thể chọn mục tiêu học tập chưa được phê duyệt.");

            if (selectedTags.Select(t => t.Id).Distinct().Count() != selectedTags.Count)
                throw new InvalidOperationException("Danh sách mục tiêu học tập không được chứa mục tiêu trùng lặp.");

            LearnerType = type;
            KnowledgeLevel = level;

            // Cập nhật danh sách mục tiêu
            _learningGoals.Clear();
            _learningGoals.AddRange(selectedTags);

            HasCompletedOnboarding = true;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Thay đổi danh sách mục tiêu học tập sau này
        /// </summary>
        public void UpdateLearningGoals(List<Tag> newTags)
        {
            if (newTags == null || !newTags.Any())
                throw new ArgumentException("Danh sách mục tiêu không được để trống.");

            if (newTags.Any(t => !t.IsPublished))
                throw new InvalidOperationException("Mục tiêu học tập chỉ được chọn trong danh sách đã có");

            if (newTags.Select(t => t.Id).Distinct().Count() != newTags.Count)
                throw new InvalidOperationException("Danh sách mục tiêu học tập không được chứa mục tiêu trùng lặp.");

            _learningGoals.Clear();
            _learningGoals.AddRange(newTags);
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
