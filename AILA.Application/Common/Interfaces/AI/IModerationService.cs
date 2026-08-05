namespace AILA.Application.Common.Interfaces.AI;
public interface IModerationService {
    Task<(bool IsSafe, string Reason)> CheckContentSafetyAsync(string input, CancellationToken cancellationToken = default);
}
