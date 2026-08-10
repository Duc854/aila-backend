using AILA.Application.Common.Exceptions;
using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using MediatR;

namespace AILA.Application.Features.PracticeAttempts.Commands.CreateAttempt;

public class CreateAttemptCommandHandler : IRequestHandler<CreateAttemptCommand, Guid>
{
    private readonly IPracticeAttemptRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateAttemptCommandHandler(IPracticeAttemptRepository repository, IUnitOfWork unitOfWork) {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateAttemptCommand request, CancellationToken cancellationToken) {
        var enrollment = await _unitOfWork.Enrollments.GetByIdAsync(request.EnrollmentId);
        if (enrollment == null)
        {
            throw new NotFoundException(nameof(Enrollment), request.EnrollmentId);
        }

        var aiPractice = await _unitOfWork.AIPracticeMaterials.GetByIdAsync(request.MaterialId);
        if (aiPractice == null)
        {
            throw new NotFoundException("Bài thực hành AI (AIPracticeMaterial) không tồn tại hoặc chưa được thiết lập kịch bản", request.MaterialId);
        }

        var attempt = new PracticeAttempt(request.EnrollmentId, request.MaterialId);
        await _repository.AddAsync(attempt, cancellationToken);
        return attempt.Id;
    }
}
