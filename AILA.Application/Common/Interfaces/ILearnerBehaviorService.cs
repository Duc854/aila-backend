using AILA.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Common.Interfaces
{
    public interface ILearnerBehaviorService
    {
        Task IncreaseScoreAsync(
            Guid learnerId,
            IEnumerable<Tag> tags,
            int score,
            CancellationToken cancellationToken = default);
    }
}
