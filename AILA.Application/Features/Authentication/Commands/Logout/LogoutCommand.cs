using AILA.Application.Common.Interfaces;
using AILA.Domain.Entities;
using FluentValidation;
using MediatR;
using Shared.Wrappers;
using System.Linq;

namespace AILA.Application.Features.Authentication.Commands.Logout
{
    public class LogoutCommand : IRequest<ResponseDto<bool>>
    {
        public string RefreshToken { get; set; } = string.Empty;
        public Guid UserId { get; set; } // Will be populated from Claims in Controller
    }

    public class LogoutCommandValidator : AbstractValidator<LogoutCommand>
    {
        public LogoutCommandValidator()
        {
            RuleFor(v => v.RefreshToken)
                .NotEmpty().WithMessage("Refresh Token không được để trống.");
        }
    }

    public class LogoutCommandHandler : IRequestHandler<LogoutCommand, ResponseDto<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;

        public LogoutCommandHandler(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher)
        {
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
        }

        public async Task<ResponseDto<bool>> Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            // Lấy danh sách token của User hiện tại
            var userTokens = await _unitOfWork.Repository<UserToken>().FindAsync(t => t.UserId == request.UserId && !t.IsRevoked);

            var tokenToRevoke = userTokens.FirstOrDefault(t => _passwordHasher.Verify(request.RefreshToken, t.RefreshTokenHash));

            if (tokenToRevoke != null)
            {
                tokenToRevoke.Revoke();
                _unitOfWork.Repository<UserToken>().Update(tokenToRevoke);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return ResponseDto<bool>.SuccessResult(true);
        }
    }
}
