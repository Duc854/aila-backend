using AILA.Application.Common.Dtos.Rag;
using AILA.Application.Common.Interfaces.AI;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Features.Rag.Commands.IndexDocumentMaterial;

public record IndexDocumentMaterialCommand(Guid MaterialId, Guid CourseId, string MaterialTitle, string ContentText) : IRequest<IndexDocumentResponseDto>;

public class IndexDocumentMaterialCommandHandler : IRequestHandler<IndexDocumentMaterialCommand, IndexDocumentResponseDto>
{
    private readonly IKnowledgeBaseService _knowledgeBaseService;

    public IndexDocumentMaterialCommandHandler(IKnowledgeBaseService knowledgeBaseService)
    {
        _knowledgeBaseService = knowledgeBaseService;
    }

    public async Task<IndexDocumentResponseDto> Handle(IndexDocumentMaterialCommand request, CancellationToken cancellationToken)
    {
        return await _knowledgeBaseService.IndexDocumentMaterialAsync(
            request.MaterialId,
            request.CourseId,
            request.MaterialTitle,
            request.ContentText,
            cancellationToken);
    }
}
