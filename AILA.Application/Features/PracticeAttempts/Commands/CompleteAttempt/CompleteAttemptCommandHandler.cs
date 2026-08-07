using AILA.Application.Common.Dtos.AI;
using AILA.Application.Common.Exceptions;
using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.AI;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Constants;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using MediatR;
using System.Linq;

namespace AILA.Application.Features.PracticeAttempts.Commands.CompleteAttempt;

public class CompleteAttemptCommandHandler : IRequestHandler<CompleteAttemptCommand, CompleteAttemptResponseDto>
{
    private readonly IPracticeAttemptRepository _repository;
    private readonly IAIPracticeMaterialRepository _materialRepo;
    private readonly IScoringService _scoringService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILearnerBehaviorService _learnerBehaviorService;

    public CompleteAttemptCommandHandler(
        IPracticeAttemptRepository repository,
        IAIPracticeMaterialRepository materialRepo,
        IScoringService scoringService,
        IUnitOfWork unitOfWork,
        ILearnerBehaviorService learnerBehaviorService)
    {
        _repository = repository;
        _materialRepo = materialRepo;
        _scoringService = scoringService;
        _unitOfWork = unitOfWork;
        _learnerBehaviorService = learnerBehaviorService;
    }

    public async Task<CompleteAttemptResponseDto> Handle(CompleteAttemptCommand request, CancellationToken cancellationToken)
    {
        var attempt = await _repository.GetByIdAsync(request.AttemptId, cancellationToken)
            ?? throw new NotFoundException(nameof(PracticeAttempt), request.AttemptId);

        var enrollment = await _unitOfWork.Enrollments
            .GetWithCourseTagsByIdAsync(
                attempt.EnrollmentId,
                cancellationToken)
            ?? throw new NotFoundException(
                nameof(Enrollment),
                attempt.EnrollmentId);
        var accountId = enrollment.LearnerId;

        var material = await _materialRepo.GetByIdAsync(attempt.MaterialId);
        var criteria = material?.ScoringCriterias.ToList() ?? new List<ScoringCriteria>();

        var validSubmissions = attempt.Submissions
            .Where(s => !s.IsRejected)
            .OrderBy(s => s.CreatedAt)
            .ToList();

        // Luôn tính toán chi tiết kết quả chấm điểm (dù attempt cũ đã bấm Complete hay mới bấm)
        var scoringResult = await _scoringService.GenerateOverallSuggestionAsync(
            validSubmissions,
            material?.Scenario ?? string.Empty,
            material?.LearnerTask ?? string.Empty,
            criteria,
            material?.AITask ?? string.Empty,
            attemptId: attempt.Id,
            accountId: accountId,
            cancellationToken: cancellationToken);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // Chỉ ghi nhận behavior ở lần đầu hoàn thành
            var firstCompleted =
                attempt.Status != PracticeAttemptStatus.Completed;

            attempt.Complete(
                scoringResult.Percentage,
                scoringResult.Summary);

            var scoringJson =
                System.Text.Json.JsonSerializer.Serialize(scoringResult);

            var aiFeedback = new AIFeedback(
                attempt.Id,
                scoringResult.Percentage,
                scoringResult.Summary,
                strengths: string.Join("; ", scoringResult.LearningSuggestions),
                areasForImprovement: string.Join("; ", scoringResult.DetectedIssues),
                detailedScoringJson: scoringJson);

            await _unitOfWork.Repository<AIFeedback>()
                .AddAsync(aiFeedback);

            if (firstCompleted)
            {
                var behaviorTags = enrollment.Course.CourseTags
                    .Where(t =>
                        !ReservedTagCodes.LevelTags.Contains(t.Code)
                        &&
                        !ReservedTagCodes.LearnerTypeTags.Contains(t.Code))
                    .ToList();

                await _learnerBehaviorService.IncreaseScoreAsync(
                    enrollment.LearnerId,
                    behaviorTags,
                    BehaviorScoreConstants.CompleteAIPractice,
                    cancellationToken);
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return new CompleteAttemptResponseDto
            {
                FinalScore = scoringResult.Percentage,
                OverallSuggestion = scoringResult.Summary,
                DetailedScoring = scoringResult
            };
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
