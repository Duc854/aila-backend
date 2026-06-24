namespace AILA.Application.Features.Blogs.Dtos
{
    public record RelatedBlogDto(
        Guid Id,
        string Title,
        string? ThumbnailUrl,
        DateTime? PublishedAt
    );

    public record BlogDetailDto(
        Guid Id,
        string Title,
        string Content,
        string? ThumbnailUrl,
        string AuthorName,
        int ViewCount,
        DateTime CreatedAt,
        DateTime? PublishedAt,
        List<RelatedBlogDto> RelatedBlogs
    );
}
