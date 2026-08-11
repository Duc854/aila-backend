using AILA.Application.Common.Exceptions;
using AILA.Application.Common.Interfaces;
using AILA.Domain.Entities;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Features.AIPricing.Commands.DeleteAIPricingConfig;

public class DeleteAIPricingConfigCommandHandler : IRequestHandler<DeleteAIPricingConfigCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteAIPricingConfigCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteAIPricingConfigCommand request, CancellationToken cancellationToken)
    {
        var setting = await _unitOfWork.Repository<AIApiCostSetting>().GetByIdAsync(request.Id);
        if (setting == null)
        {
            throw new NotFoundException(nameof(AIApiCostSetting), request.Id);
        }

        _unitOfWork.Repository<AIApiCostSetting>().Delete(setting);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
