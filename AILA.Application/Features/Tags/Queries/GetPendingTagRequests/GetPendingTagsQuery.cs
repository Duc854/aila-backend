using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AILA.Application.Features.Tags.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Tags.Queries.GetPendingTags
{
    public record GetPendingTagsQuery(
        string? SearchKeyword = null
    ) : IRequest<ResponseDto<List<PendingTagVerificationDto>>>;
}
