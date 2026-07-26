using System;
using System.Threading;
using System.Threading.Tasks;
using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Users.Dtos;
using AILA.Domain.Enums;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Users.Commands.UpdateUserRole
{
    public class UpdateUserRoleCommandHandler
        : IRequestHandler<UpdateUserRoleCommand, ResponseDto<UserDetailDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public UpdateUserRoleCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ResponseDto<UserDetailDto>> Handle(
            UpdateUserRoleCommand request,
            CancellationToken cancellationToken)
        {
            // Validate
            if (request.UserId == Guid.Empty)
            {
                return ResponseDto<UserDetailDto>.FailResult(
                    "INVALID_USER_ID",
                    "User ID không hợp lệ.");
            }

            var user = await _unitOfWork.Users.GetUserByIdAsync(
                request.UserId,
                cancellationToken);

            if (user == null)
            {
                return ResponseDto<UserDetailDto>.FailResult(
                    "USER_NOT_FOUND",
                    "Không tìm thấy tài khoản người dùng.");
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

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
    }
}