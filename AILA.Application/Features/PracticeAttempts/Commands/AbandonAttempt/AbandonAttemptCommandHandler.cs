using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Exceptions;
using AILA.Application.Common.Interfaces.AI;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using MediatR;
using System.Linq;

namespace AILA.Application.Features.PracticeAttempts.Commands.AbandonAttempt;

public class AbandonAttemptCommandHandler : IRequestHandler<AbandonAttemptCommand, Unit>
{
    private readonly IPracticeAttemptRepository _repository;
    private readonly IAIPracticeMaterialRepository _materialRepo;
    private readonly IScoringService _scoringService;
    private readonly IUnitOfWork _unitOfWork;

    public AbandonAttemptCommandHandler(
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

    public async Task<Unit> Handle(AbandonAttemptCommand request, CancellationToken cancellationToken)
    {
        var attempt = await _repository.GetByIdAsync(request.AttemptId, cancellationToken)
            ?? throw new NotFoundException(nameof(PracticeAttempt), request.AttemptId);

        var enrollment = await _unitOfWork.Enrollments.GetByIdAsync(attempt.EnrollmentId)
            ?? throw new NotFoundException(nameof(Enrollment), attempt.EnrollmentId);

        var material = await _materialRepo.GetByIdAsync(attempt.MaterialId);
        var criteria = material?.ScoringCriterias.ToList() ?? new List<ScoringCriteria>();

        var validSubmissions = attempt.Submissions
            .Where(s => !s.IsRejected)
            .OrderBy(s => s.CreatedAt)
            .ToList();

        if (validSubmissions.Any())
        {
            var scoringResult = await _scoringService.GenerateOverallSuggestionAsync(
                validSubmissions,
                material?.Scenario ?? string.Empty,
                material?.LearnerTask ?? string.Empty,
                criteria,
                material?.AITask ?? string.Empty,
                attemptId: attempt.Id,
                accountId: enrollment.LearnerId,
                cancellationToken: cancellationToken);

            attempt.Complete(scoringResult.Percentage, scoringResult.Summary);
        }
        else
        {
            attempt.Abandon();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
