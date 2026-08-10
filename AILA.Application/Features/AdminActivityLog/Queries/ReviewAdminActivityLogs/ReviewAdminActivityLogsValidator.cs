using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.AdminActivityLog.Queries.ReviewAdminActivityLogs
{
    public class ReviewAdminActivityLogsValidator
        : AbstractValidator<ReviewAdminActivityLogsQuery>
    {
        public ReviewAdminActivityLogsValidator()
        {
            RuleFor(x => x.EndDate)
                .GreaterThanOrEqualTo(x => x.StartDate)
                .When(x =>
                    x.StartDate.HasValue &&
                    x.EndDate.HasValue)
                .WithMessage(
                    "Ngày kết thúc phải sau ngày bắt đầu");
        }
    }
}
