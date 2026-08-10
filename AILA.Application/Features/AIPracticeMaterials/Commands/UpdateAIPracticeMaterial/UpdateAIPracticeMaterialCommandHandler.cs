using AILA.Application.Common.Interfaces;
using AILA.Application.Features.AIPracticeMaterials.Commands.CreateAIPracticeMaterial;
using AILA.Application.Features.AIPracticeMaterials.Queries.GetAIPracticeMaterialDetail;
using AILA.Domain.Entities;
using MediatR;
using Shared.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.AIPracticeMaterials.Commands.UpdateAIPracticeMaterial
{
    public sealed class UpdateAIPracticeMaterialCommandHandler
      : IRequestHandler<UpdateAIPracticeMaterialCommand, ResponseDto<bool>>
    {
        private readonly IUnitOfWork _uow;

        public UpdateAIPracticeMaterialCommandHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<ResponseDto<bool>> Handle(
            UpdateAIPracticeMaterialCommand command,
            CancellationToken ct)
        {
            var request = command.Request;

            // 1. Lấy AI Practice Scenario
            var aiPractice = await _uow.AIPracticeMaterials
                .GetForUpdateAsync(command.MaterialId, ct);

            if (aiPractice == null)
            {
                return ResponseDto<bool>.FailResult(
                    "AI_PRACTICE_NOT_FOUND",
                    "Không tìm thấy AI Practice Scenario.");
            }

            // 2. Kiểm tra quyền Expert
            if (aiPractice.Material.Module.Course.ExpertId != command.ExpertId)
            {
                return ResponseDto<bool>.FailResult(
                    "FORBIDDEN",
                    "Bạn không có quyền cập nhật AI Practice Scenario này.");
            }

            try
            {
                await _uow.BeginTransactionAsync(ct);

                // 3. Cập nhật Material
                aiPractice.Material.UpdateTitle(request.Title);

                // 4. Cập nhật AI Practice
                aiPractice.Update(
                    request.Scenario,
                    request.AiTask,
                    request.LearnerTask,
                    request.MaxPromptAttempts);

                // 5. Xóa dữ liệu cấu hình cũ trong Database
                await _uow.AIPracticeMaterials
                    .DeletePromptTemplatesAsync(aiPractice.MaterialId, ct);

                await _uow.AIPracticeMaterials
                    .DeleteStepGuidancesAsync(aiPractice.MaterialId, ct);

                await _uow.AIPracticeMaterials
                    .DeleteScoringCriteriaAsync(aiPractice.MaterialId, ct);

                // 6. Đồng bộ Aggregate trong Memory
                aiPractice.ClearConfiguration();

                // 7. Thêm Prompt Template mới (Easy)
                foreach (var item in request.PromptTemplates)
                {
                    aiPractice.AddPromptTemplate(
                        new PromptTemplate(
                            aiPractice.MaterialId,
                            item.Title,
                            item.Content));
                }

                // 8. Thêm Step Guidance mới (Medium)
                foreach (var item in request.StepGuidances)
                {
                    aiPractice.AddStepGuidance(
                        new StepGuidance(
                            aiPractice.MaterialId,
                            item.OrderIndex,
                            item.Content));
                }

                // 9. Thêm Scoring Criteria mới
                foreach (var item in request.ScoringCriteria)
                {
                    aiPractice.AddScoringCriteria(
                        new ScoringCriteria(
                            aiPractice.MaterialId,
                            item.Title,
                            item.Description,
                            item.Weight));
                }

                // 10. Validate Business Rules
                aiPractice.ValidateConfiguration();

                // 11. Commit
                await _uow.CommitTransactionAsync(ct);

                return ResponseDto<bool>.SuccessResult(true);
            }
            catch
            {
                await _uow.RollbackTransactionAsync(ct);
                throw;
            }
        }
    }
}
