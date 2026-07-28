using AILA.Application.Common.Interfaces;
using AILA.Domain.Entities;
using MediatR;
using Shared.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.AIPracticeMaterials.Commands.CreateAIPracticeMaterial
{
    public sealed class CreateAIPracticeMaterialCommandHandler
        : IRequestHandler<CreateAIPracticeMaterialCommand,
            ResponseDto<AIPracticeMaterialDto>>
    {
        private readonly IUnitOfWork _uow;

        public CreateAIPracticeMaterialCommandHandler(
            IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<ResponseDto<AIPracticeMaterialDto>> Handle(
            CreateAIPracticeMaterialCommand command,
            CancellationToken ct)
        {
            var request = command.Request;

            // 1. Kiểm tra Module
            var module = await _uow.Modules.GetWithCourseAsync(
                request.ModuleId,
                ct);

            if (module == null)
            {
                return ResponseDto<AIPracticeMaterialDto>.FailResult(
                    "MODULE_NOT_FOUND",
                    "Không tìm thấy chương học.");
            }

            // 2. Kiểm tra quyền Expert
            if (module.Course.ExpertId != command.ExpertId)
            {
                return ResponseDto<AIPracticeMaterialDto>.FailResult(
                    "FORBIDDEN",
                    "Bạn không có quyền thêm AI Practice Scenario.");
            }

            // 3. Tính OrderIndex
            var orderIndex = module.Materials.Any()
                ? module.Materials.Max(x => x.OrderIndex) + 1
                : 1;

            try
            {
                await _uow.BeginTransactionAsync(ct);

                // 4. Tạo Material
                var material = Material.CreateAiPractice(
                    request.ModuleId,
                    request.Title,
                    orderIndex);

                await _uow.Materials.AddAsync(material);

                // 5. Tạo AI Practice
                var aiPractice = new Domain.Entities.AIPracticeMaterial(
                    material.Id,
                    request.Scenario,
                    request.AiTask,
                    request.LearnerTask,
                    request.Difficulty,
                    request.MaxPromptAttempts);

                // 6. Prompt Templates
                foreach (var item in request.PromptTemplates)
                {
                    aiPractice.AddPromptTemplate(
                        new PromptTemplate(
                            material.Id,
                            item.Title,
                            item.Content));
                }

                // 7. Step Guidances
                foreach (var item in request.StepGuidances)
                {
                    aiPractice.AddStepGuidance(
                        new StepGuidance(
                            material.Id,
                            item.OrderIndex,
                            item.Content));
                }

                // 8. Scoring Criteria
                foreach (var item in request.ScoringCriteria)
                {
                    aiPractice.AddScoringCriteria(
                        new ScoringCriteria(
                            material.Id,
                            item.Title,
                            item.Description,
                            item.Weight));
                }

                // 9. Validate Business Rules
                aiPractice.ValidateConfiguration();

                // 10. Lưu AI Practice
                await _uow.AIPracticeMaterials.AddAsync(aiPractice);

                // 11. Commit
                await _uow.CommitTransactionAsync(ct);

                return ResponseDto<AIPracticeMaterialDto>.SuccessResult(
                    new AIPracticeMaterialDto
                    {
                        MaterialId = material.Id,
                        ModuleId = material.ModuleId,
                        Title = material.Title,
                        Difficulty = aiPractice.Difficulty,
                        MaxPromptAttempts = aiPractice.MaxPromptAttempts
                    });
            }
            catch (ArgumentException ex)
            {
                await _uow.RollbackTransactionAsync(ct);

                return ResponseDto<AIPracticeMaterialDto>.FailResult(
                    "INVALID_ARGUMENT",
                    ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                await _uow.RollbackTransactionAsync(ct);

                return ResponseDto<AIPracticeMaterialDto>.FailResult(
                    "INVALID_OPERATION",
                    ex.Message);
            }
            catch
            {
                await _uow.RollbackTransactionAsync(ct);
                throw;
            }
        }
    }
}
