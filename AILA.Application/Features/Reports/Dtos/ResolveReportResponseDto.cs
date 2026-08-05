using System;

namespace AILA.Application.Features.Reports.Dtos
{
    public class ResolveReportResponseDto
    {
        public Guid ReportId { get; set; }
        public string? Status { get; set; }
        public DateTime ResolvedAt { get; set; }
        public string? Message { get; set; }
    }
}
