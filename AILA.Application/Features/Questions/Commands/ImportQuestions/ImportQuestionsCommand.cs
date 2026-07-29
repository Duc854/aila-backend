using AILA.Application.Features.Questions.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Questions.Commands.ImportQuestions;

/// <summary>
/// Command import câu hỏi từ file Excel.
/// <br/>
/// - <b>DryRun = true</b>: Parse + validate, KHÔNG lưu DB → dùng cho bước Review.
/// - <b>DryRun = false</b>: Parse + validate + lưu DB toàn bộ dòng hợp lệ → bước Confirm.
/// </summary>
public sealed record ImportQuestionsCommand(
    Guid QuizMaterialId,
    Guid ExpertId,

    /// <summary>Stream của file .xlsx được upload.</summary>
    Stream FileStream,

    /// <summary>
    /// true  = chỉ preview, không lưu vào DB.<br/>
    /// false = import thật, lưu các dòng hợp lệ vào DB.
    /// </summary>
    bool DryRun
) : IRequest<ResponseDto<QuestionImportResultDto>>;
