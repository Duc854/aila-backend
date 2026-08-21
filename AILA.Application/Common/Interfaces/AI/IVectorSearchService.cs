using AILA.Application.Common.Dtos.Rag;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Common.Interfaces.AI
{
        public interface IVectorSearchService
        {
            Task<VectorSearchResult> SearchAsync(
                Guid courseId,
                string question,
                int topK = 3,
                double minSimilarity = 0.60,
                CancellationToken cancellationToken = default);
        }
    }

