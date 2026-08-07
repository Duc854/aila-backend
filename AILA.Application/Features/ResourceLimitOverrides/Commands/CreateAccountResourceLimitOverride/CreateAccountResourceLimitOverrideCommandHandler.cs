using AILA.Application.Common.Interfaces;
using AILA.Application.Features.ResourceLimitOverrides.Queries.GetAccountResourceLimitOverride;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using MediatR;
using Shared.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.ResourceLimitOverrides.Commands.CreateAccountResourceLimitOverride
{
    public sealed class CreateAccountResourceLimitOverrideCommandHandler
        : IRequestHandler<
            CreateAccountResourceLimitOverrideCommand,
            ResponseDto<string>>
    {
        private readonly IUnitOfWork _unitOfWork;


        public CreateAccountResourceLimitOverrideCommandHandler(
            IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }


        public async Task<ResponseDto<string>> Handle(
            CreateAccountResourceLimitOverrideCommand request,
            CancellationToken cancellationToken)
        {
            if (request.AdminId == Guid.Empty)
            {
                return ResponseDto<string>.FailResult(
                    "INVALID_ADMIN",
                    "Thông tin quản trị viên không hợp lệ.");
            }


            var account = await _unitOfWork.Users
                .GetByIdAsync(request.AccountId);


            if (account == null)
            {
                return ResponseDto<string>.FailResult(
                    "ACCOUNT_NOT_FOUND",
                    "Không tìm thấy tài khoản.");
            }


            var existingOverride =
                await _unitOfWork.AccountResourceLimits
                    .GetByAccountIdAsync(
                        request.AccountId,
                        cancellationToken);


            if (existingOverride != null)
            {
                return ResponseDto<string>.FailResult(
                    "RESOURCE_LIMIT_OVERRIDE_EXISTS",
                    "Tài khoản đã tồn tại cấu hình giới hạn tài nguyên riêng.");
            }


            var resourceLimit = new AccountResourceLimit(
                request.AccountId,
                request.AiTokenLimit,
                request.AiPracticeScenarioLimit,
                request.ExpertEvaluationRequestLimit);


            await _unitOfWork.BeginTransactionAsync(
                cancellationToken);

            try
            {
                await _unitOfWork.AccountResourceLimits
                    .AddAsync(resourceLimit);


                var activityLog = new AdminActivityLog(
                    request.AdminId,
                    AdminAction.Create,
                    nameof(AccountResourceLimit),
                    resourceLimit.Id,
                    $"Tạo cấu hình giới hạn tài nguyên riêng cho tài khoản {request.AccountId}.");


                await _unitOfWork.AdminActivityLogs
                    .AddAsync(activityLog);


                await _unitOfWork.CommitTransactionAsync(
                    cancellationToken);


                return ResponseDto<string>.SuccessResult(
                    "Tạo cấu hình giới hạn tài nguyên riêng thành công.");
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
