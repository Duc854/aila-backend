using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Common.Dtos
{
    public record BlogPostDto(
            Guid Id,
            string Title,
            string Slug,
            string? ThumbnailUrl,
            string AuthorName,
            DateTime CreatedAt
        );
}
