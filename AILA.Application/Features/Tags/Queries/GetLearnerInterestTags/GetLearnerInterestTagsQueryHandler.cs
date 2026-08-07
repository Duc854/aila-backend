using AILA.Application.Common.Dtos;
using AILA.Application.Common.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.Tags.Queries.GetLearnerInterestTags
{
    public class GetLearnerInterestTagsQueryHandler
        : IRequestHandler<GetLearnerInterestTagsQuery, List<TagDto>>
    {
        private readonly IUnitOfWork _uow;


        public GetLearnerInterestTagsQueryHandler(
            IUnitOfWork uow)
        {
            _uow = uow;
        }


        public async Task<List<TagDto>> Handle(
            GetLearnerInterestTagsQuery request,
            CancellationToken cancellationToken)
        {
            var tags = await _uow.Tags
                .GetLearnerInterestTagsAsync(cancellationToken);


            return tags.Select(t => new TagDto
            {
                Id = t.Id,
                Name = t.Name,
                Code = t.Code
            })
            .ToList();
        }
    }
}
