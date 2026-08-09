using AILA.Application.Common.Interfaces;
using AILA.Application.Features.ExpertEvaluations.Dtos;
using MediatR;
using Microsoft.Extensions.Options;
using Shared.Models;
using Shared.Wrappers;

namespace AILA.Application.Features.ExpertEvaluations.Queries.GetAssignedEvaluationRequests
{
    public sealed class GetAssignedEvaluationRequestsQueryHandler
        : IRequestHandler<GetAssignedEvaluationRequestsQuery, ResponseDto<PageResult<ExpertEvaluationRequestSummaryDto>>>
    {
        private readonly IUnitOfWork _uow;
        private readonly ExpertEvaluationSettings _settings;

        public GetAssignedEvaluationRequestsQueryHandler(
            IUnitOfWork uow,
            IOptions<ExpertEvaluationSettings> options)
        {
            _uow = uow;
            _settings = options.Value;
        }

        public async Task<ResponseDto<PageResult<ExpertEvaluationRequestSummaryDto>>> Handle(
            GetAssignedEvaluationRequestsQuery request,
            CancellationToken ct)
        {
            // E63-1: luôn phân trang và chặn client xin trang quá lớn.
            var pageIndex = Math.Max(0, request.PageIndex);
            var pageSize = Math.Clamp(
                request.PageSize ?? _settings.DefaultPageSize,
                1,
                _settings.MaxPageSize);

            // BR-01: chỉ lấy yêu cầu được giao cho chính chuyên gia này.
            var (items, totalCount) = await _uow.ExpertEvaluationRequests.GetAssignedPageAsync(
                request.ExpertId,
                request.Status,
                pageIndex,
                pageSize,
                ct);

            var materialTitles = await GetMaterialTitlesAsync(items.Select(x => x.PracticeAttempt?.MaterialId));

            var summaries = items.Select(x => new ExpertEvaluationRequestSummaryDto
            {
                RequestId = x.Id,
                PracticeAttemptId = x.PracticeAttemptId,
                LearnerId = x.LearnerId,
                LearnerName = x.Learner?.User?.FullName ?? string.Empty,
                MaterialTitle = x.PracticeAttempt is not null
                    && materialTitles.TryGetValue(x.PracticeAttempt.MaterialId, out var title)
                        ? title
                        : string.Empty,
                Status = x.Status.ToString(),
                RequestedAt = x.RequestedAt,
                AssignedAt = x.AssignedAt,
                CompletedAt = x.CompletedAt
            }).ToList();

            // AC-63.3: không có yêu cầu nào thì vẫn là 200 với danh sách rỗng.
            var page = new PageResult<ExpertEvaluationRequestSummaryDto>(
                summaries,
                totalCount,
                pageIndex,
                pageSize);

            return ResponseDto<PageResult<ExpertEvaluationRequestSummaryDto>>.SuccessResult(page);
        }

        /// <summary>
        /// Lấy tên học liệu của cả trang trong một truy vấn thay vì mỗi dòng một lần.
        /// </summary>
        private async Task<Dictionary<Guid, string>> GetMaterialTitlesAsync(IEnumerable<Guid?> materialIds)
        {
            var ids = materialIds
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

            if (ids.Count == 0)
                return new Dictionary<Guid, string>();

            var materials = await _uow.Materials.FindAsync(m => ids.Contains(m.Id));

            return materials.ToDictionary(m => m.Id, m => m.Title);
        }
    }
}
