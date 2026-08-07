using AILA.Application.Common.Dtos;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.Tags.Queries.GetLearnerInterestTags
{
    public record GetLearnerInterestTagsQuery()
        : IRequest<List<TagDto>>;
}
