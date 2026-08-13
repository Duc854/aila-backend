using FluentValidation;

namespace AILA.Application.Features.AdminBlog.Commands.UpdateBlog;

public sealed class UpdateBlogCommandValidator
    : AbstractValidator<UpdateBlogCommand>
{
    public UpdateBlogCommandValidator()
    {
        RuleFor(x => x.BlogId)
            .NotEmpty().WithMessage("Mã bài viết không được để trống.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Tiêu đề bài viết không được để trống.");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug bài viết không được để trống.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Nội dung bài viết không được để trống.");
    }
}
