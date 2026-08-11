using FluentValidation;

namespace AILA.Application.Features.AdminBlog.Commands.CreateBlog;

public sealed class CreateBlogCommandValidator
    : AbstractValidator<CreateBlogCommand>
{
    public CreateBlogCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Tiêu đề bài viết không được để trống.");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug bài viết không được để trống.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Nội dung bài viết không được để trống.");
    }
}
