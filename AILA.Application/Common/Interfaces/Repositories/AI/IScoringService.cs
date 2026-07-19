using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Common.Interfaces.Repositories.AI
{
    public interface IScoringService
    {
        Task<ScoringResult> ScoreAttemptAsync(Guid attemptId, CancellationToken ct);
    }

    public record ScoringResult(
        decimal TotalScore, decimal MaxPossible,
        string DimensionJson, string Strengths, string Weaknesses, string Suggestion);

}
