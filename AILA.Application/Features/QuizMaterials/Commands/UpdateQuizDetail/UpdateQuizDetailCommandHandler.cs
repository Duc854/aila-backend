using AILA.Application.Common.Interfaces;
using AILA.Application.Features.QuizMaterials.Dtos;
using AILA.Application.Features.QuizMaterials.Mapping;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.QuizMaterials.Commands.UpdateQuizDetail;

public sealed class UpdateQuizDetailCommandHandler
    : IRequestHandler<
        UpdateQuizDetailCommand,
        ResponseDto<QuizMaterialDto>>
{
    private readonly IUnitOfWork _uow;

    public UpdateQuizDetailCommandHandler(
        IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ResponseDto<QuizMaterialDto>> Handle(
        UpdateQuizDetailCommand request,
        CancellationToken ct)
    {
        // --------------------------------------------------
        // Đã tồn tại QuizMaterial
        // --------------------------------------------------

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
                        "Bạn không có quyền chỉnh sửa Quiz.");
            }

            if (quiz.Material.Module.Course.IsPublished)
            {
                return ResponseDto<QuizMaterialDto>
                    .FailResult(
                        "COURSE_PUBLISHED",
                        "Không thể chỉnh sửa vì khóa học đã được công khai.");
            }

            quiz.UpdateSetting(
                request.TimeLimitMinutes,
                request.PassingScore,
                request.ShowCorrectAnswersAfterSubmission);

            await _uow.SaveChangesAsync(ct);

            return ResponseDto<QuizMaterialDto>
                .SuccessResult(
                    QuizMaterialMapper.MapToDto(quiz));
        }

        // --------------------------------------------------
        // Chưa có QuizMaterial
        // -> tạo mới (Upsert)
        // --------------------------------------------------

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
                    "Bạn không có quyền chỉnh sửa Quiz.");
        }

        if (material.Module.Course.IsPublished)
        {
            return ResponseDto<QuizMaterialDto>
                .FailResult(
                    "COURSE_PUBLISHED",
                    "Không thể chỉnh sửa vì khóa học đã được công khai.");
        }

        if (material.MaterialType != MaterialType.Quiz)
        {
            return ResponseDto<QuizMaterialDto>
                .FailResult(
                    "INVALID_TYPE",
                    "Học liệu này không phải Quiz.");
        }

        var newQuiz = new QuizMaterial(
            request.MaterialId,
            request.TimeLimitMinutes,
            request.PassingScore,
            request.ShowCorrectAnswersAfterSubmission);

        await _uow
            .Repository<QuizMaterial>()
            .AddAsync(newQuiz);

        await _uow.SaveChangesAsync(ct);

        return ResponseDto<QuizMaterialDto>
            .SuccessResult(
                QuizMaterialMapper.MapToDto(newQuiz));
    }
}