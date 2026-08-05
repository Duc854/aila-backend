using AILA.Application.Common.Interfaces;
using MediatR;
using Shared.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.AIPracticeMaterials.Queries.GetAIPracticeMaterialDetail
{
    public sealed class GetAIPracticeMaterialDetailQueryHandler
        : IRequestHandler<
            GetAIPracticeMaterialDetailQuery,
            ResponseDto<AIPracticeMaterialDetailDto>>
    {
        private readonly IUnitOfWork _uow;

        public GetAIPracticeMaterialDetailQueryHandler(
            IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<ResponseDto<AIPracticeMaterialDetailDto>> Handle(
            GetAIPracticeMaterialDetailQuery request,
            CancellationToken ct)
        {
            var scenario = await _uow.AIPracticeMaterials
                .GetDetailForExpertAsync(
                    request.MaterialId,
                    ct);

            if (scenario == null)
            {
                return ResponseDto<AIPracticeMaterialDetailDto>
                    .FailResult(
                        "SCENARIO_NOT_FOUND",
                        "Không tìm thấy AI Practice Scenario.");
            }

            if (scenario.Material.Module.Course.ExpertId != request.ExpertId)
            {
                return ResponseDto<AIPracticeMaterialDetailDto>
                    .FailResult(
                        "FORBIDDEN",
                        "Bạn không có quyền truy cập AI Practice Scenario này.");
            }

            return ResponseDto<AIPracticeMaterialDetailDto>
                .SuccessResult(
                    AIPracticeMaterialMapper.MapToDto(scenario));
        }
    }
}
