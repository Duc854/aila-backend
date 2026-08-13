using AILA.Application.Common.Dtos.Rag;
using AILA.Application.Common.Interfaces.AI;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Features.Rag.Commands.SyncCourseMaterialsToRag;

public record SyncCourseMaterialsToRagCommand(Guid CourseId) : IRequest<SyncCourseRagResponseDto>;

public class SyncCourseMaterialsToRagCommandHandler : IRequestHandler<SyncCourseMaterialsToRagCommand, SyncCourseRagResponseDto>
{
    private readonly IKnowledgeBaseService _knowledgeBaseService;

    public SyncCourseMaterialsToRagCommandHandler(IKnowledgeBaseService knowledgeBaseService)
    {
        _knowledgeBaseService = knowledgeBaseService;
    }

    public async Task<SyncCourseRagResponseDto> Handle(SyncCourseMaterialsToRagCommand request, CancellationToken cancellationToken)
    {
        return await _knowledgeBaseService.SyncAllCourseMaterialsAsync(request.CourseId, cancellationToken);
    }
}
