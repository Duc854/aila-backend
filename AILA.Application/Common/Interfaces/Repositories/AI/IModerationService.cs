using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Common.Interfaces.Repositories.AI
{
    public interface IModerationService
    {
        Task<ModerationResult> ValidateAsync(string text, CancellationToken ct);
    }

    public record ModerationResult(bool IsSafe, string? ViolationType, string? Reason);
}
