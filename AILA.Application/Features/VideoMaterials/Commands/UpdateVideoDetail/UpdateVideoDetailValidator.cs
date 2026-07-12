using FluentValidation;

namespace AILA.Application.Features.VideoMaterials.Commands.UpdateVideoDetail;

public class UpdateVideoDetailValidator
    : AbstractValidator<UpdateVideoDetailCommand>
{
    public UpdateVideoDetailValidator()
    {
        RuleFor(x => x.VideoUrl)
            .NotEmpty()
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("Video URL không hợp lệ.");

        RuleFor(x => x.DurationSeconds)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.Content)
            .MaximumLength(10000);
    }
}