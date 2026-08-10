using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Users.Dtos;
using AILA.Domain.Enums;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Users.Queries.GetUsers
{
    public class GetUsersQueryHandler
        : IRequestHandler<GetUsersQuery, ResponseDto<List<UserListDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetUsersQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ResponseDto<List<UserListDto>>> Handle(
            GetUsersQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var users = await _unitOfWork.Users.GetUsersAsync(
                    request.SearchKeyword,
                    request.Role,
                    request.IsActive,
                    cancellationToken);

                var result = users.Select(u => new UserListDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    FullName = u.FullName,
                    Role = u.Role,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt
                }).ToList();

                return ResponseDto<List<UserListDto>>.SuccessResult(result);
            }
            catch (Exception ex)
            {
                return ResponseDto<List<UserListDto>>.FailResult(
                    "GET_USERS_ERROR",
                    $"Có lỗi xảy ra khi lấy danh sách users: {ex.Message}");
            }
        }
    }
}
