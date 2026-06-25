using AILA.Application.Common.Interfaces;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Features.Auth.Commands
{
    public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Guid>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;

        public RegisterCommandHandler(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher)
        {
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
        }

        public async Task<Guid> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            if (await _unitOfWork.Users.ExistsByEmailAsync(request.Email))
            {
                throw new InvalidOperationException("Email đã tồn tại."); // In a real project, throw ConflictException
            }

            var passwordHash = _passwordHasher.HashPassword(request.Password);

            var user = new User(
                email: request.Email,
                fullName: request.FullName,
                role: UserRole.Learner,
                passwordHash: passwordHash
            );

            var learner = new Learner(user.Id);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                await _unitOfWork.Users.AddAsync(user);
                await _unitOfWork.Learners.AddAsync(learner);

                await _unitOfWork.CommitTransactionAsync(cancellationToken);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }

            return user.Id;
        }
    }
}
