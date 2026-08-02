using MediatR;

namespace AILA.Application.Features.Questions.Queries.GetImportTemplate;

/// <summary>
/// Query lấy file Excel template để import câu hỏi.
/// Không cần quizMaterialId vì template là cố định.
/// ExpertId dùng để verify Expert tồn tại.
/// </summary>
public sealed record GetImportTemplateQuery(
    Guid QuizMaterialId,
    Guid ExpertId
) : IRequest<GetImportTemplateResult>;
