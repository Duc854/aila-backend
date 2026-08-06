using AILA.Application.Common.Interfaces;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using MediatR;
using Shared.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.ResourceLimitOverrides.Commands.DeleteAccountResourceLimitOverride
{
    public sealed class DeleteAccountResourceLimitOverrideCommandHandler
        : IRequestHandler<
            DeleteAccountResourceLimitOverrideCommand,
            ResponseDto<string>>
    {
        private readonly IUnitOfWork _unitOfWork;


        public DeleteAccountResourceLimitOverrideCommandHandler(
            IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }


        public async Task<ResponseDto<string>> Handle(
            DeleteAccountResourceLimitOverrideCommand request,
            CancellationToken cancellationToken)
        {
            if (request.AdminId == Guid.Empty)
            {
                return ResponseDto<string>.FailResult(
                    "INVALID_ADMIN",
                    "Thông tin quản trị viên không hợp lệ.");
            }


            if (request.AccountId == Guid.Empty)
            {
                return ResponseDto<string>.FailResult(
                    "INVALID_ACCOUNT",
                    "Thông tin tài khoản không hợp lệ.");
            }


            var resourceLimit = await _unitOfWork
                .AccountResourceLimits
                .GetByAccountIdAsync(
                    request.AccountId,
                    cancellationToken);


            if (resourceLimit == null)
            {
                return ResponseDto<string>.FailResult(
                    "ACCOUNT_RESOURCE_LIMIT_NOT_FOUND",
                    "Không tìm thấy cấu hình giới hạn tài nguyên của tài khoản.");
            }


            await _unitOfWork.BeginTransactionAsync(
                cancellationToken);


            try
            {
                _unitOfWork.AccountResourceLimits
                    .Delete(resourceLimit);


                var activityLog = new AdminActivityLog(
                    request.AdminId,
                    AdminAction.Delete,
                    nameof(AccountResourceLimit),
                    resourceLimit.Id,
                    $"Xóa cấu hình giới hạn tài nguyên riêng của tài khoản {request.AccountId}.");


                await _unitOfWork.AdminActivityLogs
                    .AddAsync(activityLog);


                await _unitOfWork.SaveChangesAsync(
                    cancellationToken);


                await _unitOfWork.CommitTransactionAsync(
                    cancellationToken);


                return ResponseDto<string>.SuccessResult(
                    "Xóa giới hạn tài nguyên riêng thành công.");
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(
                    cancellationToken);

                throw;
            }
        }
    }
}
