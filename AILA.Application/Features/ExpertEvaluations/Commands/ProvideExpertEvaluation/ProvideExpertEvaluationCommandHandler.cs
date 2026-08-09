using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Notifications;
using AILA.Application.Features.ExpertEvaluations.Dtos;
using AILA.Application.Features.ExpertEvaluations.Mapping;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Wrappers;

namespace AILA.Application.Features.ExpertEvaluations.Commands.ProvideExpertEvaluation
{
    public sealed class ProvideExpertEvaluationCommandHandler
        : IRequestHandler<ProvideExpertEvaluationCommand, ResponseDto<ExpertEvaluationResultDto>>
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<ProvideExpertEvaluationCommandHandler> _logger;

        public ProvideExpertEvaluationCommandHandler(
            IUnitOfWork uow,
            ILogger<ProvideExpertEvaluationCommandHandler> logger)
        {
            _uow = uow;
            _logger = logger;
        }

        public async Task<ResponseDto<ExpertEvaluationResultDto>> Handle(
            ProvideExpertEvaluationCommand request,
            CancellationToken ct)
        {
            var evaluationRequest = await _uow.ExpertEvaluationRequests
                .GetByIdWithEvaluationAsync(request.RequestId, ct);

            // BR-02: chuyên gia chỉ chấm được yêu cầu giao cho chính mình.
            // Không tồn tại và không được giao đều trả 404 để tránh dò ID (AF-02).
            if (evaluationRequest is null || evaluationRequest.ExpertId != request.ExpertId)
                return ResponseDto<ExpertEvaluationResultDto>.FailResult(
                    ExpertEvaluationErrors.RequestNotFound,
                    "Không tìm thấy yêu cầu đánh giá.");

            // BR-03: kết quả đã chốt là bất biến, không có đường ghi đè (AC-64.4).
            if (evaluationRequest.Status == ExpertEvaluationRequestStatus.Completed
                || evaluationRequest.ExpertEvaluation is not null)
                return ResponseDto<ExpertEvaluationResultDto>.FailResult(
                    ExpertEvaluationErrors.AlreadyEvaluated,
                    "Yêu cầu này đã có kết quả đánh giá.");

            // AC-64.6: chỉ chấm được khi yêu cầu đang ở trạng thái đã giao việc.
            if (evaluationRequest.Status != ExpertEvaluationRequestStatus.InProgress)
                return ResponseDto<ExpertEvaluationResultDto>.FailResult(
                    ExpertEvaluationErrors.InvalidState,
                    "Yêu cầu không ở trạng thái có thể chấm điểm.");

            // BR-01: kết quả chuyên gia lưu ở bảng riêng, tách khỏi đánh giá của AI.
            var evaluation = new ExpertEvaluation(
                evaluationRequest.Id,
                request.OverallScore,
                request.Feedback,
                request.Recommendation);

            await _uow.Repository<ExpertEvaluation>().AddAsync(evaluation);
            evaluationRequest.Complete();

            // Báo cho học viên đã gửi yêu cầu. LearnerId dùng chung khóa chính với User
            // nên gán thẳng làm người nhận thông báo được.
            await _uow.Notifications.AddAsync(
                NotificationTemplates.ExpertEvaluationCompleted(
                    evaluationRequest.LearnerId,
                    evaluationRequest.Id,
                    evaluation.OverallScore));

            // Tạo kết quả, chốt trạng thái và phát thông báo đi cùng một transaction nên học viên
            // không bao giờ đọc được Completed mà thiếu kết quả (E30-2).
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Chuyên gia {ExpertId} đã nộp kết quả {EvaluationId} cho yêu cầu {RequestId} với điểm {OverallScore}.",
                request.ExpertId,
                evaluation.Id,
                evaluationRequest.Id,
                evaluation.OverallScore);

            return ResponseDto<ExpertEvaluationResultDto>.SuccessResult(
                ExpertEvaluationMapper.ToExpertResult(evaluation)!);
        }
    }
}
