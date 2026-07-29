using AILA.Application.Features.AIPracticeMaterials.Commands.CreateAIPracticeMaterial;
using AILA.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.AIPracticeMaterials.Queries.GetAIPracticeMaterialDetail
{
    public static class AIPracticeMaterialMapper
    {
        public static AIPracticeMaterialDetailDto MapToDto(
            AIPracticeMaterial entity)
        {
            return new AIPracticeMaterialDetailDto
            {
                MaterialId = entity.MaterialId,
                ModuleId = entity.Material.ModuleId,

                Title = entity.Material.Title,

                Scenario = entity.Scenario,
                AiTask = entity.AITask,
                LearnerTask = entity.LearnerTask,

                Difficulty = entity.Difficulty,
                MaxPromptAttempts = entity.MaxPromptAttempts,

                PromptTemplates = entity.PromptTemplates
                    .Select(x => new PromptTemplateDto
                    {
                        Title = x.Title,
                        Content = x.Content
                    })
                    .ToList(),

                StepGuidances = entity.StepGuidances
                    .OrderBy(x => x.OrderIndex)
                    .Select(x => new StepGuidanceDto
                    {
                        OrderIndex = x.OrderIndex,
                        Content = x.Content
                    })
                    .ToList(),

                ScoringCriteria = entity.ScoringCriterias
                    .Select(x => new ScoringCriteriaDto
                    {
                        Title = x.Title,
                        Description = x.Description,
                        Weight = x.Weight
                    })
                    .ToList()
            };
        }
    }
}
