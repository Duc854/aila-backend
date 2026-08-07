using AILA.Application.Features.Authentication.Dtos;
using FluentValidation;
using MediatR;

namespace AILA.Application.Features.Authentication.Commands.ExpertLogin
{
    public record ExpertLoginCommand(string Email, string Password)
        : IRequest<LoginResponseDto?>;

    public class ExpertLoginCommandValidator : AbstractValidator<ExpertLoginCommand>
    {
        public ExpertLoginCommandValidator()
        {
            RuleFor(v => v.Email)
                .NotEmpty().WithMessage("Email không được để trống.")
                .EmailAddress().WithMessage("Email không đúng định dạng.")
                .Matches(@"^[^@\s]+@[^@\s]+\.[^@\s]+$").WithMessage("Email không đúng định dạng Regex.");

            RuleFor(v => v.Password)
                .NotEmpty().WithMessage("Mật khẩu không được để trống.");
        }
    }
}
