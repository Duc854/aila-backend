[1mdiff --git a/AILA.Application/Features/Users/Commands/UpdateUserRole/UpdateUserRoleCommandHandler.cs b/AILA.Application/Features/Users/Commands/UpdateUserRole/UpdateUserRoleCommandHandler.cs[m
[1mdeleted file mode 100644[m
[1mindex 455600e..0000000[m
[1m--- a/AILA.Application/Features/Users/Commands/UpdateUserRole/UpdateUserRoleCommandHandler.cs[m
[1m+++ /dev/null[m
[36m@@ -1,61 +0,0 @@[m
[31m-﻿using System;[m
[31m-using System.Threading;[m
[31m-using System.Threading.Tasks;[m
[31m-using AILA.Application.Common.Interfaces;[m
[31m-using AILA.Application.Features.Users.Dtos;[m
[31m-using AILA.Domain.Enums;[m
[31m-using MediatR;[m
[31m-using Shared.Wrappers;[m
[31m-[m
[31m-namespace AILA.Application.Features.Users.Commands.UpdateUserRole[m
[31m-{[m
[31m-    public class UpdateUserRoleCommandHandler[m
[31m-        : IRequestHandler<UpdateUserRoleCommand, ResponseDto<UserDetailDto>>[m
[31m-    {[m
[31m-        private readonly IUnitOfWork _unitOfWork;[m
[31m-[m
[31m-        public UpdateUserRoleCommandHandler(IUnitOfWork unitOfWork)[m
[31m-        {[m
[31m-            _unitOfWork = unitOfWork;[m
[31m-        }[m
[31m-[m
[31m-        public async Task<ResponseDto<UserDetailDto>> Handle([m
[31m-            UpdateUserRoleCommand request,[m
[31m-            CancellationToken cancellationToken)[m
[31m-        {[m
[31m-            // Validate[m
[31m-            if (request.UserId == Guid.Empty)[m
[31m-            {[m
[31m-                return ResponseDto<UserDetailDto>.FailResult([m
[31m-                    "INVALID_USER_ID",[m
[31m-                    "User ID không hợp lệ.");[m
[31m-            }[m
[31m-[m
[31m-            var user = await _unitOfWork.Users.GetUserByIdAsync([m
[31m-                request.UserId,[m
[31m-                cancellationToken);[m
[31m-[m
[31m-            if (user == null)[m
[31m-            {[m
[31m-                return ResponseDto<UserDetailDto>.FailResult([m
[31m-                    "USER_NOT_FOUND",[m
[31m-                    "Không tìm thấy tài khoản người dùng.");[m
[31m-            }[m
[31m-[m
[31m-            await _unitOfWork.SaveChangesAsync(cancellationToken);[m
[31m-[m
[31m-            var result = new UserDetailDto[m
[31m-            {[m
[31m-                Id = user.Id,[m
[31m-                Email = user.Email,[m
[31m-                FullName = user.FullName,[m
[31m-                Role = user.Role,[m
[31m-                IsActive = user.IsActive,[m
[31m-                CreatedAt = user.CreatedAt,[m
[31m-                UpdatedAt = user.UpdatedAt[m
[31m-            };[m
[31m-[m
[31m-            return ResponseDto<UserDetailDto>.SuccessResult(result);[m
[31m-        }[m
[31m-    }[m
[31m-}[m
\ No newline at end of file[m
