using FluentValidation;

namespace AILA.Application.Features.AdminBlog.Commands.CreateBlog;

public sealed class CreateBlogCommandValidator
    : AbstractValidator<CreateBlogCommand>
{
    public CreateBlogCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty();

        RuleFor(x => x.Slug)
            .NotEmpty();

        RuleFor(x => x.Content)
            .NotEmpty();
    }
}
