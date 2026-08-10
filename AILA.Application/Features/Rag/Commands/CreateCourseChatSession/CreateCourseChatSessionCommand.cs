using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Dtos.Rag;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Features.Rag.Commands.CreateCourseChatSession;

public record CreateCourseChatSessionCommand(Guid AccountId, Guid CourseId, string Title) : IRequest<CourseChatSessionDto>;

public class CreateCourseChatSessionCommandHandler : IRequestHandler<CreateCourseChatSessionCommand, CourseChatSessionDto>
{
    private readonly IKnowledgeChunkRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCourseChatSessionCommandHandler(IKnowledgeChunkRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CourseChatSessionDto> Handle(CreateCourseChatSessionCommand request, CancellationToken cancellationToken)
    {
        var session = new CourseChatSession(request.AccountId, request.CourseId, request.Title);
        await _repository.AddSessionAsync(session, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CourseChatSessionDto
        {
            Id = session.Id,
            AccountId = session.AccountId,
            CourseId = session.CourseId,
            Title = session.Title,
            CreatedAt = session.CreatedAt
        };
    }
}
