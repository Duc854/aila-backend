using AILA.Application.Common.Dtos.AI;
using AILA.Application.Common.Interfaces.Repositories;
using MediatR;
namespace AILA.Application.Features.PracticeAttempts.Queries.ListAttempts;

public class ListAttemptsQueryHandler : IRequestHandler<ListAttemptsQuery, List<PracticeAttemptDto>>
{
    private readonly IPracticeAttemptRepository _repository;
    public ListAttemptsQueryHandler(IPracticeAttemptRepository repository) { _repository = repository; }

    public async Task<List<PracticeAttemptDto>> Handle(ListAttemptsQuery request, CancellationToken cancellationToken)
    {
        var attempts = await _repository.GetByEnrollmentIdAsync(request.EnrollmentId, cancellationToken);
        return attempts.Select(a => new PracticeAttemptDto {
            Id = a.Id,
            EnrollmentId = a.EnrollmentId,
            MaterialId = a.MaterialId,
            Status = a.Status.ToString(),
            CreatedAt = a.CreatedAt,
            CompletedAt = a.CompletedAt,
            FinalScore = a.FinalScore
        }).ToList();
    }
}
