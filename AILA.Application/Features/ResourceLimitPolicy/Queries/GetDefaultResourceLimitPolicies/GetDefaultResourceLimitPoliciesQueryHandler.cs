using AILA.Application.Common.Dtos;
using AILA.Application.Common.Interfaces;
using MediatR;
using Shared.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.ResourceLimitPolicy.Queries.GetDefaultResourceLimitPolicies
{
    public sealed class GetDefaultResourceLimitPoliciesQueryHandler
        : IRequestHandler<
            GetDefaultResourceLimitPoliciesQuery,
            ResponseDto<List<ResourceLimitPolicyDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;


        public GetDefaultResourceLimitPoliciesQueryHandler(
            IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }


        public async Task<ResponseDto<List<ResourceLimitPolicyDto>>> Handle(
            GetDefaultResourceLimitPoliciesQuery request,
            CancellationToken cancellationToken)
        {
            var policies = await _unitOfWork.ResourceLimitPolicies
                .GetAllAsync();


            var result = policies
                .Select(x => new ResourceLimitPolicyDto
                {
                    AccountType = x.AccountType,

                    AiTokenLimit = x.AiTokenLimit,

                    AiPracticeScenarioLimit =
                        x.AiPracticeScenarioLimit,

                    ExpertEvaluationRequestLimit =
                        x.ExpertEvaluationRequestLimit
                })
                .OrderBy(x => x.AccountType)
                .ToList();


            return ResponseDto<List<ResourceLimitPolicyDto>>
                .SuccessResult(result);
        }
    }
}
