using System;
using System.Threading;
using System.Threading.Tasks;
using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Users.Dtos;
using AILA.Domain.Enums;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Users.Queries.GetUserById
{
    public class GetUserByIdQueryHandler
        : IRequestHandler<GetUserByIdQuery, ResponseDto<UserDetailDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetUserByIdQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ResponseDto<UserDetailDto>> Handle(
            GetUserByIdQuery request,
            CancellationToken cancellationToken)
        {
            // Validate
            if (request.UserId == Guid.Empty)
            {
                return ResponseDto<UserDetailDto>.FailResult(
                    "INVALID_USER_ID",
                    "User ID không hợp lệ.");
            }

            try
            {
                var user = await _unitOfWork.Users.GetUserWithDetailsAsync(
                    request.UserId,
                    cancellationToken);

                if (user == null)
                {
                    return ResponseDto<UserDetailDto>.FailResult(
                        "USER_NOT_FOUND",
                        "Không tìm thấy tài khoản người dùng.");
                }

                // BR-04: Admin accounts are not displayed
                if (user.Role == UserRole.Admin)
                {
                    return ResponseDto<UserDetailDto>.FailResult(
                        "ACCESS_DENIED",
                        "Không thể xem thông tin tài khoản Admin.");
                }

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
                    "GET_USER_ERROR",
                    $"Có lỗi xảy ra khi lấy thông tin user: {ex.Message}");
            }
        }
    }
}