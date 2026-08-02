using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Tags.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Tags.Queries.GetSystemTags
{
    public class GetSystemTagsQueryHandler
        : IRequestHandler<GetSystemTagsQuery, ResponseDto<List<SystemTagDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetSystemTagsQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ResponseDto<List<SystemTagDto>>> Handle(
            GetSystemTagsQuery request,
            CancellationToken cancellationToken)
        {
            var tags = await _unitOfWork.Tags
                .GetSystemTagsAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(request.SearchKeyword))
            {
                var keyword = request.SearchKeyword.Trim().ToLower();

                tags = tags
                    .Where(t => t.Name.ToLower().Contains(keyword) ||
                                t.Code.ToLower().Contains(keyword))
                    .ToList();
            }

            var result = new List<SystemTagDto>();

            foreach (var tag in tags)
            {
                var usageCount = await _unitOfWork.Tags.GetUsageCountAsync(
                    tag.Id,
                    cancellationToken);

                result.Add(new SystemTagDto
                {
                    Id = tag.Id,
                    Name = tag.Name,
                    Code = tag.Code,
                    IsPublished = tag.IsPublished,
                    Source = "System",
                    UsageCount = usageCount,
                    CreatedAt = tag.CreatedAt
                });
            }

            return ResponseDto<List<SystemTagDto>>
                .SuccessResult(result);
        }
    }
}