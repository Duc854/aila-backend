using System;
using System.Threading;
using System.Threading.Tasks;
using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Users.Dtos;
using AILA.Domain.Enums;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Users.Commands.UpdateUserStatus
{
    public class UpdateUserStatusCommandHandler
        : IRequestHandler<UpdateUserStatusCommand, ResponseDto<UserDetailDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public UpdateUserStatusCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ResponseDto<UserDetailDto>> Handle(
            UpdateUserStatusCommand request,
            CancellationToken cancellationToken)
        {
            // Validate
            if (request.UserId == Guid.Empty)
            {
                return ResponseDto<UserDetailDto>.FailResult(
                    "INVALID_USER_ID",
                    "User ID không hợp lệ.");
            }

            var user = await _unitOfWork.Users.GetByIdAsync(request.UserId);

            if (user == null)
            {
                return ResponseDto<UserDetailDto>.FailResult(
                    "USER_NOT_FOUND",
                    "Không tìm thấy tài khoản người dùng.");
            }

            // Cannot update Admin accounts
            if (user.Role == UserRole.Admin)
            {
                return ResponseDto<UserDetailDto>.FailResult(
                    "ACCESS_DENIED",
                    "Không thể cập nhật trạng thái tài khoản Admin.");
            }

            // Update status (BR-01)
            if (request.IsActive)
            {
                user.Activate();
            }
            else
            {
                user.Deactivate();
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
