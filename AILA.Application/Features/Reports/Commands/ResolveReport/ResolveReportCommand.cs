using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Reports.Commands.ResolveReport
{
    public record ResolveReportCommand(
        Guid ReportId
    ) : IRequest<ResponseDto<object>>;
}