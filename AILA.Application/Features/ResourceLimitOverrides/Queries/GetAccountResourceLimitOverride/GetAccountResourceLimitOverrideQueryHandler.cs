using AILA.Application.Common.Interfaces;
using MediatR;
using Shared.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.ResourceLimitOverrides.Queries.GetAccountResourceLimitOverride
{
    public class GetAccountResourceLimitOverrideQueryHandler
        : IRequestHandler<
            GetAccountResourceLimitOverrideQuery,
            ResponseDto<AccountResourceLimitOverrideDto>>
    {
        private readonly IUnitOfWork _unitOfWork;


        public GetAccountResourceLimitOverrideQueryHandler(
            IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }


        public async Task<ResponseDto<AccountResourceLimitOverrideDto>> Handle(
            GetAccountResourceLimitOverrideQuery request,
            CancellationToken cancellationToken)
        {
            var resourceLimit =
                await _unitOfWork.AccountResourceLimits
                    .GetByAccountIdAsync(
                        request.AccountId,
                        cancellationToken);


            if (resourceLimit == null)
            {
                return ResponseDto<AccountResourceLimitOverrideDto>
                    .SuccessResult(
                        new AccountResourceLimitOverrideDto
                        {
                            AccountId = request.AccountId,
                            HasOverride = false
                        });
            }


            var dto = new AccountResourceLimitOverrideDto
            {
                AccountId = resourceLimit.AccountId,

                HasOverride = true,

                AiTokenLimit = resourceLimit.AiTokenLimit,

                AiPracticeScenarioLimit =
                    resourceLimit.AiPracticeScenarioLimit,

                ExpertEvaluationRequestLimit =
                    resourceLimit.ExpertEvaluationRequestLimit
            };


            return ResponseDto<AccountResourceLimitOverrideDto>
                .SuccessResult(dto);
        }
    }
}
