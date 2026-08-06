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

namespace AILA.Application.Features.ResourceLimitPolicy.Commands.UpdateDefaultResourceLimitPolicies
{
    public sealed class UpdateDefaultResourceLimitPoliciesCommandHandler
       : IRequestHandler<
           UpdateDefaultResourceLimitPoliciesCommand,
           ResponseDto<string>>
    {
        private readonly IUnitOfWork _unitOfWork;


        public UpdateDefaultResourceLimitPoliciesCommandHandler(
            IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }


        public async Task<ResponseDto<string>> Handle(
            UpdateDefaultResourceLimitPoliciesCommand request,
            CancellationToken cancellationToken)
        {
            if (request.AdminId == Guid.Empty)
            {
                return ResponseDto<string>.FailResult(
                    "INVALID_ADMIN",
                    "Thông tin quản trị viên không hợp lệ.");
            }


            // Không cho phép cấu hình trùng một loại account
            var duplicateAccountType = request.Policies
                .GroupBy(x => x.AccountType)
                .Any(x => x.Count() > 1);


            if (duplicateAccountType)
            {
                return ResponseDto<string>.FailResult(
                    "DUPLICATE_RESOURCE_LIMIT_POLICY",
                    "Không được cấu hình trùng loại tài khoản.");
            }


            foreach (var policyRequest in request.Policies)
            {
                var policy = await _unitOfWork.ResourceLimitPolicies
                    .GetByAccountTypeAsync(
                        policyRequest.AccountType,
                        cancellationToken);


                if (policy == null)
                {
                    return ResponseDto<string>.FailResult(
                        "RESOURCE_LIMIT_POLICY_NOT_FOUND",
                        $"Không tìm thấy chính sách giới hạn tài nguyên cho loại tài khoản {policyRequest.AccountType}.");
                }


                policy.UpdateLimits(
                    policyRequest.AiTokenLimit,
                    policyRequest.AiPracticeScenarioLimit,
                    policyRequest.ExpertEvaluationRequestLimit);
            }


            var activityLog = new AdminActivityLog(
                request.AdminId,
                AdminAction.Update,
                nameof(ResourceLimitPolicy),
                null,
                "Cập nhật chính sách giới hạn tài nguyên mặc định.");


            await _unitOfWork.AdminActivityLogs
                .AddAsync(activityLog);


            await _unitOfWork.SaveChangesAsync(cancellationToken);


            return ResponseDto<string>.SuccessResult(
                "Cập nhật chính sách giới hạn tài nguyên mặc định thành công.");
        }
    }
}
