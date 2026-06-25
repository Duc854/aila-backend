using AILA.Application.Common.Interfaces;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Auth.Commands.Register
{
    public class RegisterCommand : IRequest<ResponseDto<Guid>>
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }

    public class RegisterCommandHandler : IRequestHandler<RegisterCommand, ResponseDto<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;

        public RegisterCommandHandler(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher)
        {
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
        }

        public async Task<ResponseDto<Guid>> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            var existingUser = await _unitOfWork.Users.GetByEmailAsync(request.Email);
            if (existingUser != null)
                return ResponseDto<Guid>.FailResult("EMAIL_EXISTS", "Email đã tồn tại.");

            var passwordHash = _passwordHasher.HashPassword(request.Password);
            var newUser = new User(request.Email, request.FullName, UserRole.Learner, passwordHash);
            
            await _unitOfWork.Users.AddAsync(newUser);
            
            var learner = new Learner(newUser.Id);
            await _unitOfWork.Learners.AddAsync(learner);
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            return ResponseDto<Guid>.SuccessResult(newUser.Id);
        }
    }
}
