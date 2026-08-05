using AILA.Application.Common.Dtos.AI;
using AILA.Application.Common.Exceptions;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Features.PracticeMaterials.Queries.GetMaterialDetail;

public class GetMaterialDetailQueryHandler : IRequestHandler<GetMaterialDetailQuery, AIPracticeMaterialDetailDto>
{
    private readonly IAIPracticeMaterialRepository _materialRepository;

    public GetMaterialDetailQueryHandler(IAIPracticeMaterialRepository materialRepository)
    {
        _materialRepository = materialRepository;
    }

    public async Task<AIPracticeMaterialDetailDto> Handle(GetMaterialDetailQuery request, CancellationToken cancellationToken)
    {
        var material = await _materialRepository.GetByIdWithDetailsAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(AIPracticeMaterial), request.Id);

        var dto = new AIPracticeMaterialDetailDto
        {
            Id = material.MaterialId,
            Title = material.Scenario.Length > 60 ? material.Scenario.Substring(0, 60) + "..." : material.Scenario,
            Scenario = material.Scenario,
            TaskDescription = material.LearnerTask,
            Difficulty = material.Difficulty,
            MaxPromptAttempts = material.MaxPromptAttempts,
            PromptTemplates = new List<PromptTemplateDto>(),
            StepGuidances = new List<StepGuidanceDto>()
        };

        if (material.PromptTemplates != null && material.PromptTemplates.Any())
        {
            dto.PromptTemplates = material.PromptTemplates.Select(pt => new PromptTemplateDto
            {
                Id = pt.Id,
                Title = pt.Title,
                Content = pt.Content
            }).ToList();
        }

        if (material.StepGuidances != null && material.StepGuidances.Any())
        {
            dto.StepGuidances = material.StepGuidances
                .GroupBy(sg => sg.OrderIndex)
                .Select(g => g.First())
                .OrderBy(sg => sg.OrderIndex)
                .Select(sg => new StepGuidanceDto
                {
                    Id = sg.Id,
                    StepOrder = sg.OrderIndex,
                    StepTitle = $"Bước {sg.OrderIndex}",
                    GuidanceText = sg.Content
                }).ToList();
        }

        return dto;
    }
}
