using AILA.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Common.Interfaces.Repositories
{
    public interface IMaterialRepository : IGenericRepository<Material>
    {
        /// <summary>
        /// Lấy thông tin chi tiết của một Material bao gồm cả thông tin Module, Video hoặc Document đi kèm
        /// </summary>
        Task<Material?> GetMaterialDetailAsync(Guid courseId, Guid materialId, CancellationToken cancellationToken = default);
        Task<bool> IsMaterialInCourseAsync(Guid materialId, Guid courseId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy Material kèm Module và Course để xác minh quyền của Expert.
        /// </summary>
        Task<Material?> GetWithModuleAndCourseAsync(
            Guid materialId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy toàn bộ Material của Module.
        /// </summary>
        Task<List<Material>> GetByModuleIdAsync(
            Guid moduleId,
            CancellationToken cancellationToken = default);

        Task<VideoMaterial?> GetVideoDetailForExpertAsync(
            Guid materialId,
            CancellationToken cancellationToken = default);

        Task<DocumentMaterial?> GetDocumentDetailForExpertAsync(
            Guid materialId,
            CancellationToken cancellationToken = default);

        Task<QuizMaterial?> GetQuizDetailForExpertAsync(
            Guid materialId,
            CancellationToken cancellationToken = default);
    }
}
