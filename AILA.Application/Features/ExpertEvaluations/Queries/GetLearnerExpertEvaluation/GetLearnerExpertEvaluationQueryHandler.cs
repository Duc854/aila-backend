using AILA.Application.Common.Interfaces;
using AILA.Application.Features.ExpertEvaluations.Dtos;
using AILA.Application.Features.ExpertEvaluations.Mapping;
using AILA.Domain.Enums;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.ExpertEvaluations.Queries.GetLearnerExpertEvaluation
{
    public sealed class GetLearnerExpertEvaluationQueryHandler
        : IRequestHandler<GetLearnerExpertEvaluationQuery, ResponseDto<LearnerExpertEvaluationDto>>
    {
        private readonly IUnitOfWork _uow;

        public GetLearnerExpertEvaluationQueryHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<ResponseDto<LearnerExpertEvaluationDto>> Handle(
            GetLearnerExpertEvaluationQuery request,
            CancellationToken ct)
        {
            var evaluationRequest = await _uow.ExpertEvaluationRequests
                .GetByIdWithEvaluationAsync(request.RequestId, ct);

            // BR-02: học viên chỉ xem được yêu cầu của chính mình (AF-01, E30-1).
            if (evaluationRequest is null || evaluationRequest.LearnerId != request.LearnerId)
                return ResponseDto<LearnerExpertEvaluationDto>.FailResult(
                    ExpertEvaluationErrors.RequestNotFound,
                    "Không tìm thấy yêu cầu đánh giá.");

            var attempt = await _uow.PracticeAttempts
                .GetByIdWithDetailsAsync(evaluationRequest.PracticeAttemptId, ct);

            var material = attempt is null
                ? null
                : await _uow.Materials.GetByIdAsync(attempt.MaterialId);

            var practiceDetail = attempt is null
                ? null
                : await _uow.AIPracticeMaterials.GetByIdWithDetailsAsync(attempt.MaterialId, ct);

            var isCompleted = evaluationRequest.Status == ExpertEvaluationRequestStatus.Completed;

            var dto = new LearnerExpertEvaluationDto
            {
                RequestId = evaluationRequest.Id,
                Status = evaluationRequest.Status.ToString(),
                RequestedAt = evaluationRequest.RequestedAt,
                AssignedAt = evaluationRequest.AssignedAt,
                CompletedAt = evaluationRequest.CompletedAt,
                Attempt = ExpertEvaluationMapper.ToAttemptContext(attempt, material, practiceDetail),
                Conversation = ExpertEvaluationMapper.ToConversation(attempt),
                AiEvaluation = ExpertEvaluationMapper.ToAiEvaluation(attempt),

                // AC-30.2/.3: chưa xong hoặc đã hủy vẫn trả 200, chỉ là không kèm kết quả chuyên gia.
                ExpertEvaluation = isCompleted
                    ? ExpertEvaluationMapper.ToExpertResult(evaluationRequest.ExpertEvaluation)
                    : null
            };

            return ResponseDto<LearnerExpertEvaluationDto>.SuccessResult(dto);
        }
    }
}
