using AILA.Domain.Entities;
using AILA.Domain.Enums;

namespace AILA.Application.Tests.Common.Builders
{
    /// <summary>
    /// Dựng <see cref="Expert"/> kèm navigation <c>User</c>. Trong runtime thật EF nạp
    /// navigation này; ở unit test phải gắn tay qua reflection (setter là private).
    /// </summary>
    public class ExpertBuilder
    {
        private readonly UserBuilder _userBuilder = new UserBuilder().WithRole(UserRole.Expert);
        private string? _bio = "Chuyên gia AI";
        private string? _specialty = "AI Literacy";
        private int _years = 5;

        public ExpertBuilder WithBio(string? bio) { _bio = bio; return this; }
        public ExpertBuilder WithSpecialty(string? s) { _specialty = s; return this; }
        public ExpertBuilder WithYears(int y) { _years = y; return this; }
        public ExpertBuilder WithEmail(string email) { _userBuilder.WithEmail(email); return this; }
        public ExpertBuilder WithFullName(string name) { _userBuilder.WithFullName(name); return this; }
        public ExpertBuilder InactiveUser() { _userBuilder.Inactive(); return this; }

        public Expert Build()
        {
            var user = _userBuilder.Build();
            var expert = new Expert(user.Id, _specialty, _years, _bio);

            TestEntity.SetProperty(expert, nameof(Expert.User), user);

            return expert;
        }
    }

    /// <summary>
    /// Dựng <see cref="Learner"/> kèm navigation <c>User</c>.
    /// </summary>
    public class LearnerBuilder
    {
        private readonly UserBuilder _userBuilder = new UserBuilder().WithRole(UserRole.Learner);
        private bool _onboarded;

        public LearnerBuilder WithEmail(string email) { _userBuilder.WithEmail(email); return this; }
        public LearnerBuilder WithFullName(string name) { _userBuilder.WithFullName(name); return this; }
        public LearnerBuilder InactiveUser() { _userBuilder.Inactive(); return this; }

        /// <summary>Đã hoàn thành onboarding — đi qua đúng API domain, không set cờ trực tiếp.</summary>
        public LearnerBuilder AlreadyOnboarded() { _onboarded = true; return this; }

        public Learner Build()
        {
            var user = _userBuilder.Build();
            var learner = new Learner(user.Id);

            TestEntity.SetProperty(learner, nameof(Learner.User), user);

            if (_onboarded)
                learner.CompleteOnboarding(
                    LearnerType.Student,
                    KnowledgeLevel.Beginner,
                    new List<Tag> { Tag.CreateByAdmin("AI Cơ bản", "AI_BASIC") });

            return learner;
        }
    }
}
