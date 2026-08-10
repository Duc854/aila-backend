using AILA.Application.Common.Dtos.AI;

namespace AILA.Application.Common.Interfaces.AI;

public interface IQuotaService {
    Task<bool> CheckAndConsumeQuotaAsync(Guid accountId, int estimatedTokens, CancellationToken cancellationToken = default);
    Task<QuotaCheckResultDto> CheckQuotaAsync(Guid accountId, int estimatedTokens, float warningThresholdPercent = 0.80f, CancellationToken cancellationToken = default);
    Task RecordTokenUsageAsync(Guid accountId, Guid? attemptId, string serviceType, string modelId, int promptTokens, int completionTokens, CancellationToken cancellationToken = default);
}
