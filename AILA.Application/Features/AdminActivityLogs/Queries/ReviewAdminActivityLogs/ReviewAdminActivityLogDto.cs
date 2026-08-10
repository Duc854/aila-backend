using AILA.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.AdminActivityLogs.Queries.ReviewAdminActivityLogs
{
    public class ReviewAdminActivityLogDto
    {
        public Guid Id { get; init; }

        public Guid AdminId { get; init; }

        public string AdminName { get; init; } = string.Empty;

        public AdminAction Action { get; init; }

        public string? Description { get; init; }

        public DateTime CreatedAt { get; init; }
    }
}
