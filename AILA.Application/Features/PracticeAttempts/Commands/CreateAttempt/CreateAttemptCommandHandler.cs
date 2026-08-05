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
        var attempt = new PracticeAttempt(request.EnrollmentId, request.MaterialId);
        await _repository.AddAsync(attempt, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return attempt.Id;
    }
}
