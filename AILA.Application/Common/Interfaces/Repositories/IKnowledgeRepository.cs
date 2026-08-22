using AILA.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Common.Interfaces.Repositories
{
    public interface IKnowledgeRepository
    {
        Task AddDocumentAsync(KnowledgeDocument doc, CancellationToken cancellationToken = default);
        Task AddChunksAsync(IEnumerable<KnowledgeChunk> chunks, CancellationToken cancellationToken = default);
        Task<KnowledgeDocument?> GetDocumentByMaterialIdAsync(Guid materialId, CancellationToken cancellationToken = default);
        Task DeleteChunksByMaterialIdAsync(Guid materialId, CancellationToken cancellationToken = default);
        Task<List<(KnowledgeChunk Chunk, double SimilarityScore)>> SearchSimilarChunksAsync(Guid courseId, float[] queryEmbedding, int topK = 5, double minSimilarity = 0.60, CancellationToken cancellationToken = default);

    }
}
