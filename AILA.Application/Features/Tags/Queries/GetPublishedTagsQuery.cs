using AILA.Application.Common.Dtos;
using MediatR;

namespace AILA.Application.Features.Tags.Queries
{
    public record GetPublishedTagsQuery() : IRequest<List<TagDto>>;
}
