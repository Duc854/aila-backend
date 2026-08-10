using AILA.Application.Common.Interfaces;
using AILA.Application.Features.ExpertEvaluations.Dtos;
using AILA.Application.Features.ExpertEvaluations.Mapping;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.ExpertEvaluations.Queries.GetAssignedEvaluationRequestDetail
{
    public sealed class GetAssignedEvaluationRequestDetailQueryHandler
        : IRequestHandler<GetAssignedEvaluationRequestDetailQuery, ResponseDto<ExpertEvaluationRequestDetailDto>>
    {
        private readonly IUnitOfWork _uow;

        public GetAssignedEvaluationRequestDetailQueryHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<ResponseDto<ExpertEvaluationRequestDetailDto>> Handle(
            GetAssignedEvaluationRequestDetailQuery request,
            CancellationToken ct)
        {
            var evaluationRequest = await _uow.ExpertEvaluationRequests
                .GetByIdWithEvaluationAsync(request.RequestId, ct);

            // BR-01: ngoài phạm vi được giao thì trả 404 như khi không tồn tại (AC-63.4).
            if (evaluationRequest is null || evaluationRequest.ExpertId != request.ExpertId)
                return ResponseDto<ExpertEvaluationRequestDetailDto>.FailResult(
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

            var learnerAccount = await _uow.Users.GetByIdAsync(evaluationRequest.LearnerId);

            var dto = new ExpertEvaluationRequestDetailDto
            {
                RequestId = evaluationRequest.Id,
                Status = evaluationRequest.Status.ToString(),
                RequestedAt = evaluationRequest.RequestedAt,
                AssignedAt = evaluationRequest.AssignedAt,
                CompletedAt = evaluationRequest.CompletedAt,
                LearnerId = evaluationRequest.LearnerId,
                LearnerName = learnerAccount?.FullName ?? string.Empty,

                // BR-02: đủ kịch bản, tiêu chí chấm, bài làm, hội thoại và kết quả AI.
                Attempt = ExpertEvaluationMapper.ToAttemptContext(attempt, material, practiceDetail),
                Conversation = ExpertEvaluationMapper.ToConversation(attempt),
                AiEvaluation = ExpertEvaluationMapper.ToAiEvaluation(attempt),
                ExpertEvaluation = ExpertEvaluationMapper.ToExpertResult(evaluationRequest.ExpertEvaluation)
            };

            return ResponseDto<ExpertEvaluationRequestDetailDto>.SuccessResult(dto);
        }
    }
}
