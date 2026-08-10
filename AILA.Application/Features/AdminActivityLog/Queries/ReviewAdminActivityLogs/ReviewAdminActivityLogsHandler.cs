using AILA.Application.Common.Interfaces;
using MediatR;
using Shared.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.AdminActivityLog.Queries.ReviewAdminActivityLogs
{
    public class ReviewAdminActivityLogsHandler
        : IRequestHandler<
            ReviewAdminActivityLogsQuery,
            ResponseDto<IReadOnlyList<ReviewAdminActivityLogDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public ReviewAdminActivityLogsHandler(
            IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ResponseDto<IReadOnlyList<ReviewAdminActivityLogDto>>> Handle(
            ReviewAdminActivityLogsQuery request,
            CancellationToken cancellationToken)
        {
            var logs =
                await _unitOfWork.AdminActivityLogs
                    .GetLogsWithActionAsync(
                        request.StartDate,
                        request.EndDate,
                        request.Action);

            var result = logs
                .Select(x => new ReviewAdminActivityLogDto
                {
                    Id = x.Id,
                    AdminId = x.AdminId,
                    AdminName = x.Admin.FullName,
                    Action = x.Action,
                    Description = x.Description,
                    CreatedAt = x.CreatedAt
                })
                .ToList();

            return ResponseDto<IReadOnlyList<ReviewAdminActivityLogDto>>
                .SuccessResult(result);
        }
    }
}
