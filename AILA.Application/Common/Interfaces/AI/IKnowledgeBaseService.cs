using AILA.Application.Common.Dtos.Rag;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Common.Interfaces.AI;

public interface IKnowledgeBaseService
{
    Task<IndexDocumentResponseDto> IndexDocumentMaterialAsync(Guid materialId, Guid courseId, string materialTitle, string contentText, CancellationToken cancellationToken = default);
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
}
