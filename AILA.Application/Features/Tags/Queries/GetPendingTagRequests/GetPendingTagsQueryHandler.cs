using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Tags.Dtos;
using AILA.Domain.Enums;
using MediatR;
using Shared.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Features.Tags.Queries.GetPendingTags
{
    public class GetPendingTagsQueryHandler
        : IRequestHandler<GetPendingTagsQuery, ResponseDto<List<PendingTagVerificationDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetPendingTagsQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ResponseDto<List<PendingTagVerificationDto>>> Handle(
            GetPendingTagsQuery request,
            CancellationToken cancellationToken)
        {
            var tags = await _unitOfWork.Tags.GetPendingVerificationRequestsAsync(cancellationToken);

            // Apply search filter if provided
            if (!string.IsNullOrWhiteSpace(request.SearchKeyword))
            {
                var keyword = request.SearchKeyword.Trim().ToLower();
                tags = tags
                    .Where(t => t.Name.ToLower().Contains(keyword) ||
                                t.Code.ToLower().Contains(keyword))
                    .ToList();
            }

            var result = new List<PendingTagVerificationDto>();

            foreach (var tag in tags)
            {
                string submittedBy = "System";

                if (tag.CreatedById.HasValue)
                {
                    var user = await _unitOfWork.Users.GetByIdAsync(tag.CreatedById.Value);
                    submittedBy = user?.FullName ?? "Unknown";
                }

                result.Add(new PendingTagVerificationDto
                {
                    Id = tag.Id,
                    Name = tag.Name,
                    Code = tag.Code,
                    CreatedById = tag.CreatedById,
                    SubmittedBy = submittedBy,
                    RequestStatus = tag.PublishRequest?.Status,
                    SubmittedAt = tag.PublishRequest?.CreatedAt
                });
            }

            return ResponseDto<List<PendingTagVerificationDto>>
                .SuccessResult(result);
        }
    }
}