using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Users.Dtos;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Users.Commands.CreateExpertAccount
{
    public class CreateExpertAccountCommandHandler
        : IRequestHandler<CreateExpertAccountCommand, ResponseDto<UserDetailDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;

        public CreateExpertAccountCommandHandler(
            IUnitOfWork unitOfWork,
            IPasswordHasher passwordHasher)
        {
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
        }

        public async Task<ResponseDto<UserDetailDto>> Handle(
            CreateExpertAccountCommand request,
            CancellationToken cancellationToken)
        {
            // Validate FullName (BR-01)
            if (string.IsNullOrWhiteSpace(request.FullName))
            {
                return ResponseDto<UserDetailDto>.FailResult(
                    "INVALID_FULL_NAME",
                    "Họ tên không được để trống.");
            }

            // Validate Email (BR-01)
            if (string.IsNullOrWhiteSpace(request.Email) ||
                !new EmailAddressAttribute().IsValid(request.Email))
            {
                return ResponseDto<UserDetailDto>.FailResult(
                    "INVALID_EMAIL",
                    "Email không hợp lệ.");
            }

            // Validate Password (BR-01)
            if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            {
                return ResponseDto<UserDetailDto>.FailResult(
                    "INVALID_PASSWORD",
                    "Mật khẩu phải có ít nhất 6 ký tự.");
            }

            try
            {
                // Check duplicate email (BR-02)
                var normalizedEmail = request.Email.Trim().ToLowerInvariant();
                var emailExists = await _unitOfWork.Users.EmailExistsAsync(
                    normalizedEmail,
                    cancellationToken);

                if (emailExists)
                {
                    return ResponseDto<UserDetailDto>.FailResult(
                        "DUPLICATE_EMAIL",
                        "Email đã tồn tại.");
                }

                // Create User (BR-03)
                var passwordHash = _passwordHasher.HashPassword(request.Password);
                var user = new User(
                    normalizedEmail,
                    request.FullName.Trim(),
                    UserRole.Expert,
                    passwordHash
                );

                // Create Expert Profile
                var expert = new Expert(user.Id);

                // Save in transaction
                await _unitOfWork.Users.CreateUserWithExpertProfileAsync(
                    user,
                    expert,
                    cancellationToken);

                var result = new UserDetailDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    Role = user.Role,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt
                };

                return ResponseDto<UserDetailDto>.SuccessResult(result);
            }
            catch (Exception ex)
            {
                return ResponseDto<UserDetailDto>.FailResult(
                    "CREATE_EXPERT_ERROR",
                    $"Có lỗi xảy ra khi tạo tài khoản chuyên gia: {ex.Message}");
            }
        }
    }
}
