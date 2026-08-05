using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Authentication.Dtos;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using FluentValidation;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Authentication.Commands.Register
{
    public class RegisterCommand : IRequest<ResponseDto<RegisterResponseDto>>
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }

    public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
    {
        public RegisterCommandValidator()
        {
            RuleFor(v => v.Email)
                .NotEmpty().WithMessage("Email không được để trống.")
                .EmailAddress().WithMessage("Email không đúng định dạng.");

            RuleFor(v => v.Password)
                .NotEmpty().WithMessage("Mật khẩu không được để trống.");
                
            RuleFor(v => v.FullName)
                .NotEmpty().WithMessage("Họ tên không được để trống.");
        }
    }

    public class RegisterCommandHandler : IRequestHandler<RegisterCommand, ResponseDto<RegisterResponseDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;

        public RegisterCommandHandler(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher)
        {
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
        }

        public async Task<ResponseDto<RegisterResponseDto>> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            var existingUser = await _unitOfWork.Users.GetByEmailAsync(request.Email);
            if (existingUser != null)
                return ResponseDto<RegisterResponseDto>.FailResult("EMAIL_EXISTS", "Email đã tồn tại.");

            var passwordHash = _passwordHasher.HashPassword(request.Password);
            var newUser = new User(request.Email, request.FullName, UserRole.Learner, passwordHash);

            await _unitOfWork.Users.AddAsync(newUser);

            var learner = new Learner(newUser.Id);
            await _unitOfWork.Learners.AddAsync(learner);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ResponseDto<RegisterResponseDto>.SuccessResult(new RegisterResponseDto { UserId = newUser.Id });
        }
    }
}
