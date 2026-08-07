using AILA.Application.Common.Interfaces;
using AILA.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Common.InternalService
{
    public class LearnerBehaviorService
        : ILearnerBehaviorService
    {
        private readonly IUnitOfWork _unitOfWork;


        public LearnerBehaviorService(
            IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }


        public async Task IncreaseScoreAsync(
            Guid learnerId,
            IEnumerable<Tag> tags,
            int score,
            CancellationToken cancellationToken = default)
        {
            if (score <= 0)
                throw new ArgumentException(
                    "Behavior score phải lớn hơn 0.",
                    nameof(score));


            var tagList = tags.ToList();

            if (!tagList.Any())
                return;


            var tagIds = tagList
                .Select(x => x.Id)
                .ToList();


            // 1 query duy nhất
            var existingScores =
                await _unitOfWork.LearnerTagScores
                    .GetByLearnerIdAndTagIdsAsync(
                        learnerId,
                        tagIds,
                        cancellationToken);


            var scoreDictionary =
                existingScores
                    .ToDictionary(
                        x => x.TagId);


            foreach (var tag in tagList)
            {
                if (!scoreDictionary.TryGetValue(
                        tag.Id,
                        out var learnerTagScore))
                {
                    learnerTagScore =
                        new LearnerTagScore(
                            learnerId,
                            tag.Id);

                    await _unitOfWork.LearnerTagScores
                        .AddAsync(learnerTagScore);
                }


                learnerTagScore
                    .IncreaseBehaviorScore(score);
            }
        }
    }
}
