using AILA.Application.Common.Dtos;
using AILA.Application.Common.Interfaces;
using MediatR;
using Shared.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.ResourceLimitOverrides.Queries.GetOverrideEligibleAccounts
{
    public class GetOverrideEligibleAccountsQueryHandler
        : IRequestHandler<
            GetOverrideEligibleAccountsQuery,
            ResponseDto<PageResult<AccountOverrideAccountDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;


        public GetOverrideEligibleAccountsQueryHandler(
            IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }


        public async Task<ResponseDto<PageResult<AccountOverrideAccountDto>>> Handle(
            GetOverrideEligibleAccountsQuery request,
            CancellationToken cancellationToken)
        {
            var result = await _unitOfWork.Users
                .GetOverrideEligibleAccountsAsync(
                    request.Keyword,
                    request.PageRequest,
                    cancellationToken);


            var pageResult = new PageResult<AccountOverrideAccountDto>(
                result.Items,
                result.TotalItems,
                request.PageRequest.PageIndex,
                request.PageRequest.PageSize);


            return ResponseDto<PageResult<AccountOverrideAccountDto>>
                .SuccessResult(pageResult);
        }
    }
}
