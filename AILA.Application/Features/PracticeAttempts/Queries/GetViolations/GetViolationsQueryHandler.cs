using AILA.Application.Common.Dtos.AI;
using AILA.Application.Common.Exceptions;
using AILA.Application.Common.Interfaces.Repositories;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Features.PracticeAttempts.Queries.GetViolations;

public class GetViolationsQueryHandler : IRequestHandler<GetViolationsQuery, List<PromptViolationLogDto>>
{
    private readonly IPracticeAttemptRepository _attemptRepository;

    public GetViolationsQueryHandler(IPracticeAttemptRepository attemptRepository)
    {
        _attemptRepository = attemptRepository;
    }

    public async Task<List<PromptViolationLogDto>> Handle(GetViolationsQuery request, CancellationToken cancellationToken)
    {
        var attempt = await _attemptRepository.GetByIdAsync(request.AttemptId, cancellationToken);

        if (attempt == null) return new List<PromptViolationLogDto>();

        return attempt.Submissions
            .Where(s => s.IsRejected)
            .Select(s => new PromptViolationLogDto
            {
                Id = s.Id,
                SubmissionId = s.Id,
                ViolationReason = s.RejectionReason ?? "Vi phạm chính sách",
                PolicyName = s.PolicyName ?? "SafetyPolicy",
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            })
            .ToList();
    }
}
