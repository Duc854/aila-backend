using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Tags.Dtos;
using AILA.Domain.Entities;
using MediatR;
using Shared.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Features.Tags.Queries.GetSystemTags
{
    public class GetSystemTagsQueryHandler
        : IRequestHandler<GetSystemTagsQuery, ResponseDto<List<TagDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetSystemTagsQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ResponseDto<List<TagDto>>> Handle(
            GetSystemTagsQuery request,
            CancellationToken cancellationToken)
        {
            // Dùng method có sẵn trong ITagRepository
            var tags = await _unitOfWork.Tags.GetSystemTagsAsync(cancellationToken);

            // Apply search filter if provided (filter in memory)
            if (!string.IsNullOrWhiteSpace(request.SearchKeyword))
            {
                var keyword = request.SearchKeyword.Trim().ToLower();
                tags = tags
                    .Where(t => t.Name.ToLower().Contains(keyword) ||
                                t.Code.ToLower().Contains(keyword))
                    .ToList();
            }

            var result = tags.Select(tag => new TagDto
            {
                Id = tag.Id,
                Name = tag.Name,
                Code = tag.Code,
                IsPublished = tag.IsPublished,
                CreatedById = tag.CreatedById,
                Source = "System",
                UsageCount = 0 // TODO: Tính usage count từ CourseTag
            }).ToList();

            return ResponseDto<List<TagDto>>.SuccessResult(result);
        }
    }
}