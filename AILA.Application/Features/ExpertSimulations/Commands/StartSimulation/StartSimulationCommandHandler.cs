using AILA.Application.Common.Exceptions;
using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.AI;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Features.ExpertSimulations.Commands.StartSimulation;

public class StartSimulationCommandHandler : IRequestHandler<StartSimulationCommand, Guid>
{
    private readonly IAIPracticeMaterialRepository _materialRepo;
    private readonly IQuotaService _quotaService;
    private readonly IUnitOfWork _unitOfWork;

    public StartSimulationCommandHandler(
        IAIPracticeMaterialRepository materialRepo,
        IQuotaService quotaService,
        IUnitOfWork unitOfWork)
    {
        _materialRepo = materialRepo;
        _quotaService = quotaService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(StartSimulationCommand request, CancellationToken cancellationToken)
    {
        // Step 1.1: Verify Expert User exists in DB
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(request.ExpertId);
        if (user == null)
        {
            throw new NotFoundException("User (Expert)", request.ExpertId);
        }

        // Step 2 (AF-01): Load Material draft configuration
        var material = await _materialRepo.GetByIdAsync(request.MaterialId)
            ?? throw new NotFoundException("AIPracticeMaterial", request.MaterialId);

        // Step 3 (AF-02 / BR-01): Check Expert AI Tokens
        var quotaCheck = await _quotaService.CheckQuotaAsync(request.ExpertId, 1000, 0.80f, cancellationToken);
        if (!quotaCheck.IsAllowed)
        {
            throw new InvalidOperationException("AF-02: Hạn mức Token AI của Expert không đủ để khởi tạo simulation.");
        }

        // Step 4 (BR-02): Create new ExpertSimulationAttempt
        var simulationAttempt = new ExpertSimulationAttempt(request.ExpertId, request.MaterialId);
        await _unitOfWork.Repository<ExpertSimulationAttempt>().AddAsync(simulationAttempt);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return simulationAttempt.Id;
    }
}
