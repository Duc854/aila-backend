using AILA.Application.Common.Dtos.AI;
using AILA.Application.Common.Exceptions;
using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.AI;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Features.ExpertSimulations.Commands.CompleteSimulation;

public class CompleteSimulationCommandHandler : IRequestHandler<CompleteSimulationCommand, CompleteAttemptResponseDto>
{
    private readonly IAIPracticeMaterialRepository _materialRepo;
    private readonly IScoringService _scoringService;
    private readonly IUnitOfWork _unitOfWork;

    public CompleteSimulationCommandHandler(
        IAIPracticeMaterialRepository materialRepo,
        IScoringService scoringService,
        IUnitOfWork unitOfWork)
    {
        _materialRepo = materialRepo;
        _scoringService = scoringService;
        _unitOfWork = unitOfWork;
    }

    public async Task<CompleteAttemptResponseDto> Handle(CompleteSimulationCommand request, CancellationToken cancellationToken)
    {
        // 1. Fetch ExpertSimulationAttempt
        var simulation = await _unitOfWork.Repository<ExpertSimulationAttempt>()
            .GetByIdAsync(request.SimulationSessionId);

        if (simulation == null)
        {
            throw new NotFoundException(nameof(ExpertSimulationAttempt), request.SimulationSessionId);
        }

        // 2. Load Material & Scoring Criteria
        var material = await _materialRepo.GetByIdAsync(simulation.MaterialId);
        var criteria = material?.ScoringCriterias.ToList() ?? new List<ScoringCriteria>();

        // 3. Load Submissions for this simulation session
        var submissions = await _unitOfWork.Repository<PromptSubmission>()
            .FindAsync(s => s.AttemptId == simulation.Id);

        var validSubmissions = submissions
            .Where(s => !s.IsRejected)
            .OrderBy(s => s.CreatedAt)
            .ToList();

        // 4. Run AI Scoring & Evaluation for Simulation Session
        var scoringResult = await _scoringService.GenerateOverallSuggestionAsync(
            validSubmissions,
            material?.Scenario ?? string.Empty,
            material?.LearnerTask ?? string.Empty,
            criteria,
            material?.AITask ?? string.Empty,
            attemptId: simulation.Id,
            accountId: simulation.ExpertId,
            cancellationToken: cancellationToken);

        // 5. Complete Expert Simulation Session
        simulation.Complete(scoringResult.Percentage, scoringResult.Summary);

        // 6. Save AI Feedback for Expert Simulation
        var scoringJson = System.Text.Json.JsonSerializer.Serialize(scoringResult);
        var aiFeedback = new AIFeedback(
            simulation.Id,
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
