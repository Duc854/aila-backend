using AILA.Application.Common.Dtos.AI;
using AILA.Application.Common.Interfaces;
using AILA.Domain.Entities;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Features.PracticeAttempts.Queries.GetViolations;

public class GetViolationsQueryHandler : IRequestHandler<GetViolationsQuery, List<PromptViolationLogDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetViolationsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<PromptViolationLogDto>> Handle(GetViolationsQuery request, CancellationToken cancellationToken)
    {
        var attempt = await _unitOfWork.Repository<PracticeAttempt>().GetByIdAsync(request.AttemptId);
        if (attempt == null) return new List<PromptViolationLogDto>();

        var enrollment = await _unitOfWork.Enrollments.GetByIdAsync(attempt.EnrollmentId);
        if (enrollment == null) return new List<PromptViolationLogDto>();

        var violations = await _unitOfWork.Repository<UserViolationRecord>()
            .FindAsync(v => v.UserId == enrollment.LearnerId);

        return violations
            .Select(v => new PromptViolationLogDto
            {
                Id = v.Id,
                SubmissionId = v.Id,
                ViolationReason = v.Reason,
                PolicyName = v.PolicyName,
                ViolatingPrompt = v.ViolatingPrompt,
                CreatedAt = v.CreatedAt,
                UpdatedAt = v.UpdatedAt
            })
            .ToList();
    }
}
