using AILA.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Domain.Entities
{
    public class AIPracticeMaterial
    {
        private readonly List<PromptTemplate> _promptTemplates = new();
        private readonly List<StepGuidance> _stepGuidances = new();
        private readonly List<ScoringCriteria> _scoringCriterias = new();

        public Guid MaterialId { get; private set; }

        public string Scenario { get; private set; }

        public string AITask { get; private set; }

        public string LearnerTask { get; private set; }

        public PracticeDifficulty Difficulty { get; private set; }

        public int MaxPromptAttempts { get; private set; }

        // Navigation
        public virtual Material Material { get; private set; } = null!;

        public virtual IReadOnlyCollection<PromptTemplate> PromptTemplates
            => _promptTemplates.AsReadOnly();

        public virtual IReadOnlyCollection<StepGuidance> StepGuidances
            => _stepGuidances.AsReadOnly();

        public virtual IReadOnlyCollection<ScoringCriteria> ScoringCriterias
            => _scoringCriterias.AsReadOnly();

        private AIPracticeMaterial() { }

        public AIPracticeMaterial(
            Guid materialId,
            string scenario,string aiTask, string learnerTask,
            PracticeDifficulty difficulty,
            int maxPromptAttempts)
        {
            if (materialId == Guid.Empty)
                throw new ArgumentException("Material không hợp lệ.");

            if (string.IsNullOrWhiteSpace(scenario))
                throw new ArgumentException("Mô tả kịch bản không được để trống.");
            if (string.IsNullOrWhiteSpace(aiTask))
                throw new ArgumentException("Mô tả nhiệm vụ của AI trong kịch bản không được để trống.");
            if (string.IsNullOrWhiteSpace(learnerTask))
                throw new ArgumentException("Mô tả mục tiêu của người học trong kịch bản không được để trống.");

            if (maxPromptAttempts <= 0)
                throw new ArgumentException("Số lượt prompt tối đa phải lớn hơn 0.");

            MaterialId = materialId;
            Scenario = scenario.Trim();
            AITask = aiTask.Trim();
            LearnerTask = learnerTask.Trim();
            Difficulty = difficulty;
            MaxPromptAttempts = maxPromptAttempts;
        }

        public void Update(
            string scenario, string aiTask, string learnerTask,
            int maxPromptAttempts)
        {

            if (string.IsNullOrWhiteSpace(scenario))
                throw new ArgumentException("Mô tả kịch bản không được để trống.");
            if (string.IsNullOrWhiteSpace(aiTask))
                throw new ArgumentException("Mô tả nhiệm vụ của AI trong kịch bản không được để trống.");
            if (string.IsNullOrWhiteSpace(learnerTask))
                throw new ArgumentException("Mô tả mục tiêu của người học trong kịch bản không được để trống.");

            if (maxPromptAttempts <= 0)
                throw new ArgumentException("Số lượt prompt tối đa phải lớn hơn 0.");

            Scenario = scenario.Trim();
            AITask = aiTask.Trim();
            LearnerTask = learnerTask.Trim();
            MaxPromptAttempts = maxPromptAttempts;
        }

        public void AddPromptTemplate(PromptTemplate template)
        {
            ArgumentNullException.ThrowIfNull(template);

            if (Difficulty != PracticeDifficulty.Easy)
                throw new InvalidOperationException("Chỉ kịch bản mức Dễ mới được phép có Prompt Template.");

            _promptTemplates.Add(template);
        }

        public void RemovePromptTemplate(Guid promptTemplateId)
        {
            var template = _promptTemplates.FirstOrDefault(x => x.Id == promptTemplateId)
                ?? throw new ArgumentException("Không tìm thấy Prompt Template.");

            _promptTemplates.Remove(template);
        }

        public void AddStepGuidance(StepGuidance guidance)
        {
            ArgumentNullException.ThrowIfNull(guidance);

            if (Difficulty != PracticeDifficulty.Medium)
                throw new InvalidOperationException("Chỉ kịch bản mức Trung bình mới được phép có Step Guidance.");

            _stepGuidances.Add(guidance);
        }

        public void RemoveStepGuidance(Guid stepGuidanceId)
        {
            var guidance = _stepGuidances.FirstOrDefault(x => x.Id == stepGuidanceId)
                ?? throw new ArgumentException("Không tìm thấy Step Guidance.");

            _stepGuidances.Remove(guidance);
        }

        public void AddScoringCriteria(ScoringCriteria criteria)
        {
            ArgumentNullException.ThrowIfNull(criteria);
            _scoringCriterias.Add(criteria);
        }

        public void RemoveScoringCriteria(Guid criteriaId)
        {
            var criteria = _scoringCriterias.FirstOrDefault(x => x.Id == criteriaId)
                ?? throw new ArgumentException("Không tìm thấy tiêu chí chấm điểm.");
            _scoringCriterias.Remove(criteria);
        }

        public void ValidateScoringCriteria()
        {
            if (!_scoringCriterias.Any())
                throw new InvalidOperationException(
                    "Kịch bản thực hành phải có ít nhất một tiêu chí chấm điểm.");

            var totalWeight = _scoringCriterias.Sum(x => x.Weight);

            if (totalWeight != 100)
                throw new InvalidOperationException(
                    $"Tổng trọng số các tiêu chí hiện tại là {totalWeight}%. Tổng trọng số phải bằng 100%.");
        }

        public void ValidateConfiguration()
        {
            ValidateScoringCriteria();

            switch (Difficulty)
            {
                case PracticeDifficulty.Easy:
                    if (!_promptTemplates.Any())
                        throw new InvalidOperationException(
                            "Kịch bản mức Dễ phải có ít nhất một Prompt Template.");
                    break;

                case PracticeDifficulty.Medium:
                    if (!_stepGuidances.Any())
                        throw new InvalidOperationException(
                            "Kịch bản mức Trung bình phải có ít nhất một Step Guidance.");
                    break;

                case PracticeDifficulty.Hard:
                    if (_promptTemplates.Any() || _stepGuidances.Any())
                        throw new InvalidOperationException(
                            "Kịch bản mức Khó không được chứa Prompt Template hoặc Step Guidance.");
                    break;
            }
        }

        public void ClearConfiguration()
        {
            _promptTemplates.Clear();
            _stepGuidances.Clear();
            _scoringCriterias.Clear();
        }
    }
}
