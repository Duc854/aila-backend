using AILA.Domain.Enums;
using MediatR;
using Shared.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.AdminActivityLogs.Queries.ReviewAdminActivityLogs
{
    public class ReviewAdminActivityLogsQuery
        : IRequest<ResponseDto<IReadOnlyList<ReviewAdminActivityLogDto>>>
    {
        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public AdminAction? Action { get; set; }
    }
}
