using AILA.Application.Features.Reports.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Reports.Commands.LockCourseFromReport;

/// <summary>
/// Admin lock course liên quan đến report.
/// Đồng thời resolve report và gọi Course.LockVisibility().
/// </summary>
public sealed record LockCourseFromReportCommand(Guid ReportId)
    : IRequest<ResponseDto<CourseModerationResponseDto>>;
