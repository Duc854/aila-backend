using AILA.Application.Common.Dtos.AI;
using AILA.Application.Common.Exceptions;
using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.AI;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using MediatR;

namespace AILA.Application.Features.ExpertSimulations.Queries.GetSimulationDetail;

public sealed class GetSimulationDetailQueryHandler
    : IRequestHandler<GetSimulationDetailQuery, PracticeAttemptDto>
{
    private readonly IUnitOfWork _uow;
    private readonly IAIPracticeMaterialRepository _materialRepo;
    private readonly IScoringService _scoringService;

    public GetSimulationDetailQueryHandler(
        IUnitOfWork uow,
        IAIPracticeMaterialRepository materialRepo,
        IScoringService scoringService)
    {
        _uow = uow;
        _materialRepo = materialRepo;
        _scoringService = scoringService;
    }

    public async Task<PracticeAttemptDto> Handle(
        GetSimulationDetailQuery request,
        CancellationToken cancellationToken)
    {
        var simulation = await _uow.Repository<ExpertSimulationAttempt>()
            .GetByIdAsync(request.SimulationId)
            ?? throw new NotFoundException(nameof(ExpertSimulationAttempt), request.SimulationId);

        var material   = await _materialRepo.GetByIdAsync(simulation.MaterialId);
        var criteria   = material?.ScoringCriterias.ToList() ?? new List<ScoringCriteria>();

        // Load submissions cho simulation
        var submissions = (await _uow.Repository<PromptSubmission>()
            .FindAsync(s => s.AttemptId == simulation.Id))
            .OrderBy(s => s.CreatedAt)
            .ToList();

        OverallScoringResult? detailedScoring = null;

        if (simulation.Status == PracticeAttemptStatus.Completed)
        {
            // Ưu tiên đọc AIFeedback đã lưu
            var savedFeedback = (await _uow.Repository<AIFeedback>()
                .FindAsync(f => f.AttemptId == simulation.Id))
                .FirstOrDefault();

            if (savedFeedback != null && !string.IsNullOrEmpty(savedFeedback.DetailedScoringJson))
            {
                try
                {
                    detailedScoring = System.Text.Json.JsonSerializer
                        .Deserialize<OverallScoringResult>(savedFeedback.DetailedScoringJson);
                }
                catch { }
            }

            // Fallback: gọi lại ScoringService nếu chưa có
            if (detailedScoring == null)
            {
                var validSubs = submissions.Where(s => !s.IsRejected).ToList();
                detailedScoring = await _scoringService.GenerateOverallSuggestionAsync(
                    validSubs,
                    material?.Scenario ?? string.Empty,
                    material?.LearnerTask ?? string.Empty,
                    criteria,
                    material?.AITask ?? string.Empty,
                    attemptId: simulation.Id,
                    accountId: simulation.ExpertId,
                    cancellationToken: cancellationToken);
            }
        }

        return new PracticeAttemptDto
        {
            Id               = simulation.Id,
            EnrollmentId     = Guid.Empty, // simulation không có EnrollmentId
            MaterialId       = simulation.MaterialId,
            Status           = simulation.Status.ToString(),
            CreatedAt        = simulation.CreatedAt,
            CompletedAt      = simulation.CompletedAt,
            FinalScore       = simulation.FinalScore,
            OverallSuggestion = simulation.OverallSuggestion,
            DetailedScoring  = detailedScoring,
            Submissions      = submissions.Select(s => new PromptSubmissionDto
            {
                Id               = s.Id,
                UserPrompt       = s.UserPrompt,
                AiResponse       = s.AiResponse,
                Status           = s.IsRejected ? "Violation" : "Success",
                IsViolation      = s.IsRejected,
                ViolationMessage = s.RejectionReason,
                CreatedAt        = s.CreatedAt
            }).ToList()
        };
    }
}
