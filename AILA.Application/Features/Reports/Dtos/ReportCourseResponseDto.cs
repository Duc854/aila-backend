using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.Reports.Dtos
{
    public record ReportCourseResponseDto(
        Guid ReportId,
        string Status,
        DateTime CreatedAt
    );
    
}
