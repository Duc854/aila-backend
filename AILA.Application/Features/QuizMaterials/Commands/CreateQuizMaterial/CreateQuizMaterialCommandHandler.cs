using AILA.Application.Common.Interfaces;
using AILA.Application.Features.QuizMaterials.Dtos;
using AILA.Application.Features.QuizMaterials.Mapping;
using AILA.Domain.Entities;
using MediatR;
using Shared.Wrappers;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Features.QuizMaterials.Commands.CreateQuizMaterial;

public sealed class CreateQuizMaterialCommandHandler
    : IRequestHandler<CreateQuizMaterialCommand, ResponseDto<QuizMaterialDto>>
{
    private readonly IUnitOfWork _uow;

    public CreateQuizMaterialCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ResponseDto<QuizMaterialDto>> Handle(
        CreateQuizMaterialCommand request,
        CancellationToken ct)
    {
        var module = await _uow.Modules.GetWithCourseAsync(request.ModuleId, ct);
        if (module == null)
        {
            return ResponseDto<QuizMaterialDto>.FailResult(
                "MODULE_NOT_FOUND",
                "Không tìm thấy chương học.");
        }

        if (module.Course.ExpertId != request.ExpertId)
        {
            return ResponseDto<QuizMaterialDto>.FailResult(
                "FORBIDDEN",
                "Bạn không có quyền thêm học liệu.");
        }

        if (module.Course.IsPublished)
        {
            return ResponseDto<QuizMaterialDto>.FailResult(
                "COURSE_NOT_MODIFIABLE",
                "Không thể thêm học liệu khi khóa học đang ở trạng thái công khai.");
        }

        var nextOrderIndex = module.Materials.Any()
            ? module.Materials.Max(x => x.OrderIndex) + 1
            : 1;

        try
        {
            await _uow.BeginTransactionAsync(ct);

            var material = Material.CreateQuiz(request.ModuleId, request.Title, nextOrderIndex);
            await _uow.Materials.AddAsync(material);

            var quiz = new QuizMaterial(
                material.Id,
                request.TimeLimitMinutes,
                request.PassingScore,
                request.ShowCorrectAnswersAfterSubmission);
            await _uow.Repository<QuizMaterial>().AddAsync(quiz);

            await _uow.CommitTransactionAsync(ct);

            return ResponseDto<QuizMaterialDto>.SuccessResult(
                QuizMaterialMapper.MapToDto(quiz));
        }
        catch (Exception ex)
        {
            await _uow.RollbackTransactionAsync(ct);
            return ResponseDto<QuizMaterialDto>.FailResult(
                "CREATE_FAILED",
                ex.Message);
        }
    }
}
