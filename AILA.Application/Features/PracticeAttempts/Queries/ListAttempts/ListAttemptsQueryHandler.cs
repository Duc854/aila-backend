using AILA.Application.Common.Dtos.AI;
using AILA.Application.Common.Exceptions;
using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using MediatR;
namespace AILA.Application.Features.PracticeAttempts.Queries.ListAttempts;

public class ListAttemptsQueryHandler : IRequestHandler<ListAttemptsQuery, List<PracticeAttemptDto>>
{
    private readonly IPracticeAttemptRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public ListAttemptsQueryHandler(IPracticeAttemptRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<PracticeAttemptDto>> Handle(ListAttemptsQuery request, CancellationToken cancellationToken)
    {
        if (request.RequestAccountId != Guid.Empty)
        {
            var enrollment = await _unitOfWork.Enrollments.GetByIdAsync(request.EnrollmentId);
            if (enrollment == null)
            {
                throw new NotFoundException(nameof(Enrollment), request.EnrollmentId);
            }

            if (enrollment.LearnerId != request.RequestAccountId)
            {
                throw new ForbiddenAccessException("Bạn không có quyền xem danh sách phiên luyện tập của ghi danh này.");
            }
        }

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
