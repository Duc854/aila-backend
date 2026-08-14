using AILA.Application.Common.Interfaces;
using AILA.Domain.Entities;
using MediatR;
using Shared.Wrappers;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Features.AIPricing.Commands.DeleteAIPricingConfig;

public class DeleteAIPricingConfigCommandHandler : IRequestHandler<DeleteAIPricingConfigCommand, ResponseDto<bool>>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteAIPricingConfigCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ResponseDto<bool>> Handle(DeleteAIPricingConfigCommand request, CancellationToken cancellationToken)
    {
        var setting = await _unitOfWork.Repository<AIApiCostSetting>().GetByIdAsync(request.Id);
        if (setting == null)
            return ResponseDto<bool>.FailResult("NOT_FOUND", "Không tìm thấy cấu hình giá.");

        _unitOfWork.Repository<AIApiCostSetting>().Delete(setting);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ResponseDto<bool>.SuccessResult(true);
    }
}
