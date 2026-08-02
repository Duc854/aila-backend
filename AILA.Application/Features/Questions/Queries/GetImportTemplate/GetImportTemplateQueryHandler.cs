using AILA.Application.Common.Interfaces;
using MediatR;

namespace AILA.Application.Features.Questions.Queries.GetImportTemplate;

public sealed class GetImportTemplateQueryHandler
    : IRequestHandler<GetImportTemplateQuery, GetImportTemplateResult>
{
    private readonly IUnitOfWork _uow;
    private readonly IQuestionExcelService _excelService;

    public GetImportTemplateQueryHandler(
        IUnitOfWork uow,
        IQuestionExcelService excelService)
    {
        _uow = uow;
        _excelService = excelService;
    }

    public async Task<GetImportTemplateResult> Handle(
        GetImportTemplateQuery request,
        CancellationToken ct)
    {
        // Verify quiz tồn tại và thuộc expert
        var quiz = await _uow.Materials
            .GetQuizDetailForExpertAsync(request.QuizMaterialId, ct);

        if (quiz == null)
            return GetImportTemplateResult.Fail("QUIZ_NOT_FOUND", "Không tìm thấy Quiz.");

        if (quiz.Material.Module.Course.ExpertId != request.ExpertId)
            return GetImportTemplateResult.Fail("FORBIDDEN", "Bạn không có quyền truy cập Quiz này.");

        var fileBytes = _excelService.GenerateImportTemplate();

        return GetImportTemplateResult.Ok(fileBytes);
    }
}
