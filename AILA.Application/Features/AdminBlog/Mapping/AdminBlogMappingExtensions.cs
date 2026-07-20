using AILA.Application.Features.AdminBlog.DTOs;
using AILA.Domain.Entities;

namespace AILA.Application.Features.AdminBlog.Mapping
{
    public static class AdminBlogMappingExtensions
    {
        public static AdminBlogDto MapToDto(this BlogPost blog)
        {
            return new AdminBlogDto
            {
                Id = blog.Id,
                Title = blog.Title,
                Slug = blog.Slug,
                Content = blog.Content,
                ThumbnailUrl = blog.ThumbnailUrl,
                IsPublished = blog.IsPublished,
                PublishedAt = blog.PublishedAt,
                ViewCount = blog.ViewCount,
                CreatedAt = blog.CreatedAt,
                UpdatedAt = blog.UpdatedAt
            };
        }

        public static AdminBlogListItemDto MapToListItemDto(this BlogPost blog)
        {
            return new AdminBlogListItemDto
            {
                Id = blog.Id,
                Title = blog.Title,
                Slug = blog.Slug,
                ThumbnailUrl = blog.ThumbnailUrl,
                IsPublished = blog.IsPublished,
                ViewCount = blog.ViewCount,
                CreatedAt = blog.CreatedAt,
                PublishedAt = blog.PublishedAt
            };
        }

        public static IEnumerable<AdminBlogListItemDto> MapToListItemDtos(
            this IEnumerable<BlogPost> blogs)
        {
            return blogs.Select(x => x.MapToListItemDto());
        }
    }
}