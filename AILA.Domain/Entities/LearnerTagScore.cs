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
            LearnerId = learnerId;
            TagId = tagId;
            ProfileSeed = profileSeed;
            BehaviorScore = 0;
        }

        public void IncreaseBehaviorScore(int score)
        {
            if (score <= 0)
                throw new ArgumentException(nameof(score));

            BehaviorScore += score;
            UpdateTimestamp();
        }

        public void ResetBehaviorScore()
        {
            BehaviorScore = 0;
            UpdateTimestamp();
        }

        public void UpdateProfileSeed(int score)
        {
            ProfileSeed = score;
            UpdateTimestamp();
        }
    }
}
