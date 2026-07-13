using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AILA.Application.Common.Interfaces;
using AILA.Domain.Enums;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Reports.Commands.ResolveReport
{

        public class ResolveReportCommandHandler(IUnitOfWork uow)
            : IRequestHandler<ResolveReportCommand, ResponseDto<object>>
        {
            public async Task<ResponseDto<object>> Handle(
                ResolveReportCommand request,
                CancellationToken ct)
            {
                var report = await uow.ContentReports.GetByIdAsync(request.ReportId);

                if (report == null)
                {
                    return ResponseDto<object>.FailResult(
                        "REPORT_NOT_FOUND",
                        "Không tìm thấy báo cáo.");
                }

                // BR-04
                if (report.Status != ReportStatus.Pending)
                {
                    return ResponseDto<object>.FailResult(
                        "ALREADY_RESOLVED",
                        "Báo cáo đã được xử lý.");
                }

                report.Resolve();

                await uow.SaveChangesAsync(ct);

                return ResponseDto<object>.SuccessResult(new
                {
                    Message = "Report resolved successfully."
                });
            }
        }
    }

