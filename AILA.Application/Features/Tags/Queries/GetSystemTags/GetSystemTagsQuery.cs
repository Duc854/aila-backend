using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AILA.Application.Features.Tags.Dtos;
using MediatR;
using Shared.Wrappers;
using System;
using System.Collections.Generic;

namespace AILA.Application.Features.Tags.Queries.GetSystemTags
{
    public record GetSystemTagsQuery(
        string? SearchKeyword = null
    ) : IRequest<ResponseDto<List<TagDto>>>;
}