using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Dtos.AI;
using AILA.Application.Common.Exceptions;
using AILA.Application.Common.Interfaces.AI;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using MediatR;
using System.Linq;

namespace AILA.Application.Features.PracticeAttempts.Queries.GetAttemptDetail;

public class GetAttemptDetailQueryHandler : IRequestHandler<GetAttemptDetailQuery, PracticeAttemptDto>
{
    private readonly IPracticeAttemptRepository _repository;
    private readonly IAIPracticeMaterialRepository _materialRepo;
    private readonly IScoringService _scoringService;
    private readonly IUnitOfWork _unitOfWork;

    public GetAttemptDetailQueryHandler(
        IPracticeAttemptRepository repository,
        IAIPracticeMaterialRepository materialRepo,
        IScoringService scoringService,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _materialRepo = materialRepo;
        _scoringService = scoringService;
        _unitOfWork = unitOfWork;
    }

    public async Task<PracticeAttemptDto> Handle(GetAttemptDetailQuery request, CancellationToken cancellationToken)
    {
        var attempt = await _repository.GetByIdAsync(request.AttemptId, cancellationToken)
            ?? throw new NotFoundException(nameof(PracticeAttempt), request.AttemptId);

        var enrollment = await _unitOfWork.Enrollments.GetByIdAsync(attempt.EnrollmentId)
            ?? throw new NotFoundException(nameof(Enrollment), attempt.EnrollmentId);

        if (request.RequestAccountId != Guid.Empty && enrollment.LearnerId != request.RequestAccountId)
        {
            throw new ForbiddenAccessException("Bạn không có quyền xem chi tiết phiên luyện tập này.");
        }

        var material = await _materialRepo.GetByIdWithDetailsAsync(attempt.MaterialId, cancellationToken);
        var criteriaList = material?.ScoringCriterias.ToList() ?? new List<ScoringCriteria>();

        OverallScoringResult? detailedScoring = null;

        if (attempt.Status == PracticeAttemptStatus.Completed)
        {
            // 1. Đọc AIFeedback đã lưu trong DB trước
            var savedFeedback = (await _unitOfWork.Repository<AIFeedback>()
                .FindAsync(f => f.AttemptId == attempt.Id))
                .FirstOrDefault();

            if (savedFeedback != null && !string.IsNullOrEmpty(savedFeedback.DetailedScoringJson))
            {
                try
                {
                    detailedScoring = System.Text.Json.JsonSerializer.Deserialize<OverallScoringResult>(savedFeedback.DetailedScoringJson);
                }
                catch { }
            }

            // 2. Nếu chưa có trong DB thì fallback gọi ScoringService
            if (detailedScoring == null)
            {
                var validSubmissions = attempt.Submissions
                    .OrderBy(s => s.CreatedAt)
                    .ToList();

                detailedScoring = await _scoringService.GenerateOverallSuggestionAsync(
                    validSubmissions,
                    material?.Scenario ?? string.Empty,
                    material?.LearnerTask ?? string.Empty,
                    criteriaList,
                    material?.AITask ?? string.Empty,
                    attemptId: attempt.Id,
                    accountId: enrollment.LearnerId,
                    cancellationToken: cancellationToken);
            }
        }

        // UC-29/30: cho FE biết lượt này đã nhờ chuyên gia đánh giá hay chưa, để màn kết quả
        // hiện link xem đánh giá thay vì nút gửi yêu cầu mới (backend sẽ chặn bằng BR-02).
        var evaluationRequest = await _unitOfWork.ExpertEvaluationRequests
            .GetActiveRequestForAttemptAsync(attempt.Id, cancellationToken);

        return new PracticeAttemptDto
        {
            Id = attempt.Id,
            EnrollmentId = attempt.EnrollmentId,
            MaterialId = attempt.MaterialId,
            Status = attempt.Status.ToString(),
            CreatedAt = attempt.CreatedAt,
            CompletedAt = attempt.CompletedAt,
            FinalScore = attempt.FinalScore,
            OverallSuggestion = attempt.OverallSuggestion,
            DetailedScoring = detailedScoring,
            ExpertEvaluationRequestId = evaluationRequest?.Id,
            ExpertEvaluationStatus = evaluationRequest?.Status.ToString(),
            Submissions = attempt.Submissions.Select(s => new PromptSubmissionDto
            {
                Id = s.Id,
                UserPrompt = s.UserPrompt,
                AiResponse = s.AiResponse,
                Status = "Success",
                IsViolation = false,
                ViolationMessage = null,
                CreatedAt = s.CreatedAt
            }).ToList()
        };
    }
}
