using AILA.Application.Common.Interfaces;
using AILA.Application.Features.QuizMaterials.Dtos;
using AILA.Application.Features.QuizMaterials.Mapping;
using AILA.Domain.Enums;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.QuizMaterials.Queries.GetQuizDetail;

public sealed class GetQuizDetailQueryHandler
    : IRequestHandler<
        GetQuizDetailQuery,
        ResponseDto<QuizMaterialDto>>
{
    private readonly IUnitOfWork _uow;

    public GetQuizDetailQueryHandler(
        IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ResponseDto<QuizMaterialDto>> Handle(
        GetQuizDetailQuery request,
        CancellationToken ct)
    {
        // 1. Đã có QuizMaterial?
        var quiz = await _uow.Materials
            .GetQuizDetailForExpertAsync(
                request.MaterialId,
                ct);

        if (quiz != null)
        {
            if (quiz.Material.Module.Course.ExpertId != request.ExpertId)
            {
                return ResponseDto<QuizMaterialDto>
                    .FailResult(
                        "FORBIDDEN",
                        "Bạn không có quyền truy cập Quiz này.");
            }

            return ResponseDto<QuizMaterialDto>
                .SuccessResult(
                    QuizMaterialMapper.MapToDto(quiz));
        }

        // 2. Chưa có QuizMaterial
        var material = await _uow.Materials
            .GetWithModuleAndCourseAsync(
                request.MaterialId,
                ct);

        if (material == null)
        {
            return ResponseDto<QuizMaterialDto>
                .FailResult(
                    "MATERIAL_NOT_FOUND",
                    "Không tìm thấy học liệu.");
        }

        if (material.Module.Course.ExpertId != request.ExpertId)
        {
            return ResponseDto<QuizMaterialDto>
                .FailResult(
                    "FORBIDDEN",
                    "Bạn không có quyền truy cập Quiz này.");
        }

        if (material.MaterialType != MaterialType.Quiz)
        {
            return ResponseDto<QuizMaterialDto>
                .FailResult(
                    "INVALID_TYPE",
                    "Học liệu này không phải Quiz.");
        }

        // Trả DTO mặc định để FE hiển thị form tạo lần đầu
        var dto = new QuizMaterialDto
        {
            MaterialId = material.Id,

            Title = material.Title,

            TimeLimitMinutes = 30,

            PassingScore = 70,

            ShowCorrectAnswersAfterSubmission = true
        };

        return ResponseDto<QuizMaterialDto>
            .SuccessResult(dto);
    }
}
