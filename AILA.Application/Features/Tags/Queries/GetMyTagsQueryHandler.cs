using AILA.Application.Common.Dtos;
using AILA.Application.Common.Interfaces;
using MediatR;

namespace AILA.Application.Features.Tags.Queries
{
    public class GetMyTagsQueryHandler
        : IRequestHandler<GetMyTagsQuery, List<ExpertTagDto>>
    {
        private readonly IUnitOfWork _uow;

        public GetMyTagsQueryHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<List<ExpertTagDto>> Handle(
            GetMyTagsQuery request,
            CancellationToken cancellationToken)
        {
            var tags = await _uow.Tags.GetByExpertAsync(request.ExpertId, cancellationToken);

            return tags.Select(t => new ExpertTagDto
            {
                Id          = t.Id,
                Name        = t.Name,
                Code        = t.Code,
                IsPublished = t.IsPublished,
                CreatedAt   = t.CreatedAt,
                PublishRequest = t.PublishRequest is null ? null : new TagPublishRequestDto
                {
                    Id         = t.PublishRequest.Id,
                    Status     = t.PublishRequest.Status.ToString(),
                    Note       = t.PublishRequest.Note,
                    CreatedAt  = t.PublishRequest.CreatedAt,
                    ReviewedAt = t.PublishRequest.ReviewedAt
                }
            }).ToList();
        }
    }
}
