using AILA.Application.Features.ExpertEvaluations.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.ExpertEvaluations.Commands.ProvideExpertEvaluation
{
    /// <summary>
    /// UC-64: chuyên gia nộp điểm và phản hồi, chốt yêu cầu sang Completed.
    /// <paramref name="ExpertId"/> luôn lấy từ token, không nhận từ payload.
    /// </summary>
    public record ProvideExpertEvaluationCommand(
        Guid RequestId,
        Guid ExpertId,
        decimal OverallScore,
        string Feedback,
        string? Recommendation) : IRequest<ResponseDto<ExpertEvaluationResultDto>>;
}
