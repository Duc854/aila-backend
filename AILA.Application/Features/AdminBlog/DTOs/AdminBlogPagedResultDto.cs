using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.AdminBlog.DTOs
{
    public class AdminBlogPagedResultDto
    {
        public IEnumerable<AdminBlogListItemDto> Items { get; set; }
            = Enumerable.Empty<AdminBlogListItemDto>();

        public int TotalCount { get; set; }

        public int PageNumber { get; set; }

        public int PageSize { get; set; }
    }
}
