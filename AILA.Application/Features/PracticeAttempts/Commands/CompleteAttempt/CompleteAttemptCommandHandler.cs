using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Dtos.AI;
using AILA.Application.Common.Exceptions;
using AILA.Application.Common.Interfaces.AI;
using AILA.Application.Common.Interfaces.Repositories;
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

    public CompleteAttemptCommandHandler(
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

    public async Task<CompleteAttemptResponseDto> Handle(CompleteAttemptCommand request, CancellationToken cancellationToken)
    {
        var attempt = await _repository.GetByIdAsync(request.AttemptId, cancellationToken)
            ?? throw new NotFoundException(nameof(PracticeAttempt), request.AttemptId);

        var enrollment = await _unitOfWork.Enrollments.GetByIdAsync(attempt.EnrollmentId)
            ?? throw new NotFoundException(nameof(Enrollment), attempt.EnrollmentId);
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

        // Luôn cập nhật lại OverallSuggestion & FinalScore mới nhất với kết quả chấm điểm mới
        attempt.Complete(scoringResult.Percentage, scoringResult.Summary);

        // Lưu nhận xét AI chi tiết vào bảng AIFeedback
        var scoringJson = System.Text.Json.JsonSerializer.Serialize(scoringResult);
        var aiFeedback = new AIFeedback(
            attempt.Id,
            scoringResult.Percentage,
            scoringResult.Summary,
            strengths: string.Join("; ", scoringResult.LearningSuggestions),
            areasForImprovement: string.Join("; ", scoringResult.DetectedIssues),
            detailedScoringJson: scoringJson);

        await _unitOfWork.Repository<AIFeedback>().AddAsync(aiFeedback);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CompleteAttemptResponseDto
        {
            FinalScore = scoringResult.Percentage,
            OverallSuggestion = scoringResult.Summary,
            DetailedScoring = scoringResult
        };
    }
}
