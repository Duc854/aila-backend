using AILA.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Domain.Entities
{
    public class LearnerTagScore : BaseEntity
    {
        private const int MaxBehaviorScore = 1000;
        private const int MaxProfileSeed = 200;
        public Guid LearnerId { get; private set; }
        public Guid TagId { get; private set; }

        public int ProfileSeed { get; private set; }
        public int BehaviorScore { get; private set; }

        public int RawScore => ProfileSeed + BehaviorScore;

        public virtual Learner Learner { get; private set; } = null!;
        public virtual Tag Tag { get; private set; } = null!;

        private LearnerTagScore() { }

        public LearnerTagScore(Guid learnerId, Guid tagId, int profileSeed = 0)
        {
            if (learnerId == Guid.Empty)
                throw new ArgumentException(
                    "Mã người học không hợp lệ.",
                    nameof(learnerId));

            if (tagId == Guid.Empty)
                throw new ArgumentException(
                    "Mã tag không hợp lệ.",
                    nameof(tagId));

            if (profileSeed < 0 || profileSeed > MaxProfileSeed)
                throw new ArgumentOutOfRangeException(
                    nameof(profileSeed),
                    $"Điểm hồ sơ tối đa là {MaxProfileSeed}.");

            LearnerId = learnerId;
            TagId = tagId;
            ProfileSeed = profileSeed;
            BehaviorScore = 0;
        }

        public void IncreaseBehaviorScore(int score)
        {
            if (score <= 0)
                throw new ArgumentException(
                    "Điểm hành vi phải lớn hơn 0.",
                    nameof(score));

            BehaviorScore = Math.Min(
                BehaviorScore + score,
                MaxBehaviorScore);

            UpdateTimestamp();
        }

        public void ResetBehaviorScore()
        {
            BehaviorScore = 0;
            UpdateTimestamp();
        }

        public void UpdateProfileSeed(int score)
        {
            if (score < 0 || score > MaxProfileSeed)
                throw new ArgumentOutOfRangeException(nameof(score),$"Điểm tối đa cho chủ đề mong muốn theo hồ sơ là {MaxProfileSeed}.");

            ProfileSeed = score;
            UpdateTimestamp();
        }
    }
}
