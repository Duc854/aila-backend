using FluentValidation;

namespace AILA.Application.Features.AdminBlog.Commands.UpdateBlog;

public sealed class UpdateBlogCommandValidator
    : AbstractValidator<UpdateBlogCommand>
{
    public UpdateBlogCommandValidator()
    {
        RuleFor(x => x.BlogId)
            .NotEmpty();

        RuleFor(x => x.Title)
            .NotEmpty();

        RuleFor(x => x.Slug)
            .NotEmpty();

        RuleFor(x => x.Content)
            .NotEmpty();
    }
}