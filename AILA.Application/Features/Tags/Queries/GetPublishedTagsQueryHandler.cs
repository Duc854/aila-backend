using AILA.Application.Common.Dtos;
using AILA.Application.Common.Interfaces;
using MediatR;

namespace AILA.Application.Features.Tags.Queries
{
    public class GetPublishedTagsQueryHandler
        : IRequestHandler<GetPublishedTagsQuery, List<TagDto>>
    {
        private readonly IUnitOfWork _uow;

        public GetPublishedTagsQueryHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<List<TagDto>> Handle(
            GetPublishedTagsQuery request,
            CancellationToken cancellationToken)
        {
            var tags = await _uow.Tags
                .GetPublishedSelectableTagsAsync(
                    cancellationToken);

            return tags.Select(t => new TagDto
            {
                Id = t.Id,
                Name = t.Name,
                Code = t.Code
            }).ToList();
        }
    }
}
