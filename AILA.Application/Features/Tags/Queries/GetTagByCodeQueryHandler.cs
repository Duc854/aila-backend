using AILA.Application.Features.Tags.Dtos;
using AILA.Application.Common.Interfaces;
using MediatR;

namespace AILA.Application.Features.Tags.Queries
{
    public class GetTagByCodeQueryHandler : IRequestHandler<GetTagByCodeQuery, TagDto?>
    {
        private readonly IUnitOfWork _uow;

        public GetTagByCodeQueryHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<TagDto?> Handle(
            GetTagByCodeQuery request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Code))
                return null;

            var tag = await _uow.Tags.GetByCodeAsync(
                request.Code.ToLower().Trim(),
                cancellationToken);

            if (tag is null)
                return null;

            return new TagDto
            {
                Id          = tag.Id,
                Name        = tag.Name,
                Code        = tag.Code,
                IsPublished = tag.IsPublished,
                CreatedById = tag.CreatedById,
                Source      = tag.CreatedById is null ? "System" : "Custom",
            };
        }
    }
}
