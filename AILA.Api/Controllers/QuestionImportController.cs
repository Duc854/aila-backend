using AILA.Api.Extensions;
using AILA.Application.Features.Questions.Commands.ImportQuestions;
using AILA.Application.Features.Questions.Dtos;
using AILA.Application.Features.Questions.Queries.GetImportTemplate;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Wrappers;

namespace AILA.Api.Controllers;

/// <summary>
/// Các endpoint hỗ trợ import câu hỏi hàng loạt từ file Excel.
/// Route đặt cùng gốc với QuestionsController để URL nhất quán.
/// </summary>
[Authorize(Roles = "Expert")]
[ApiController]
[Route("api/quiz-materials/{quizMaterialId:guid}/questions")]
public class QuestionImportController : ControllerBase
{
    private readonly ISender _sender;

    public QuestionImportController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Download file Excel template mẫu để điền câu hỏi import.
    /// GET /api/quiz-materials/{quizMaterialId}/questions/import-template
    /// </summary>
    [HttpGet("import-template")]
    public async Task<IActionResult> DownloadImportTemplate(
        Guid quizMaterialId,
        CancellationToken ct)
    {
        var identity = HttpContext.GetUserIdentity();
        if (identity is null)
            return Unauthorized(ResponseDto<object>.FailResult("AUTH_FAILED", "Xác thực thất bại."));

        var query  = new GetImportTemplateQuery(quizMaterialId, identity.UserId);
        var result = await _sender.Send(query, ct);

        if (!result.Success)
        {
            return result.ErrorCode switch
            {
                "QUIZ_NOT_FOUND" => NotFound(ResponseDto<object>.FailResult(result.ErrorCode!, result.ErrorMessage!)),
                "FORBIDDEN"      => StatusCode(StatusCodes.Status403Forbidden,
                                        ResponseDto<object>.FailResult(result.ErrorCode!, result.ErrorMessage!)),
                _                => BadRequest(ResponseDto<object>.FailResult(result.ErrorCode!, result.ErrorMessage!))
            };
        }

        const string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        const string fileName    = "question_import_template.xlsx";

        return File(result.FileContent!, contentType, fileName);
    }

    /// <summary>
    /// Preview file import — parse và validate KHÔNG lưu vào DB.
    /// Expert dùng để review trước khi confirm.
    /// POST /api/quiz-materials/{quizMaterialId}/questions/import/preview
    /// </summary>
    [HttpPost("import/preview")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB
    public async Task<IActionResult> PreviewImport(
        Guid quizMaterialId,
        IFormFile file,
        CancellationToken ct)
    {
        var identity = HttpContext.GetUserIdentity();
        if (identity is null)
            return Unauthorized(ResponseDto<object>.FailResult("AUTH_FAILED", "Xác thực thất bại."));

        if (file is null || file.Length == 0)
            return BadRequest(ResponseDto<object>.FailResult("NO_FILE", "Vui lòng chọn file để upload."));

        if (!IsExcelFile(file.FileName))
            return BadRequest(ResponseDto<object>.FailResult("INVALID_FILE_TYPE", "Chỉ chấp nhận file .xlsx"));

        await using var stream = file.OpenReadStream();

        var command = new ImportQuestionsCommand(
            quizMaterialId,
            identity.UserId,
            stream,
            DryRun: true);

        var result = await _sender.Send(command, ct);

        if (!result.Success)
        {
            return result.ErrorCode switch
            {
                "QUIZ_NOT_FOUND" => NotFound(result),
                "FORBIDDEN"      => StatusCode(StatusCodes.Status403Forbidden, result),
                "INVALID_FILE"   => BadRequest(result),
                _                => BadRequest(result)
            };
        }

        return Ok(result);
    }

    /// <summary>
    /// Confirm import — parse, validate và lưu tất cả dòng hợp lệ vào DB.
    /// POST /api/quiz-materials/{quizMaterialId}/questions/import/confirm
    /// </summary>
    [HttpPost("import/confirm")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB
    public async Task<IActionResult> ConfirmImport(
        Guid quizMaterialId,
        IFormFile file,
        CancellationToken ct)
    {
        var identity = HttpContext.GetUserIdentity();
        if (identity is null)
            return Unauthorized(ResponseDto<object>.FailResult("AUTH_FAILED", "Xác thực thất bại."));

        if (file is null || file.Length == 0)
            return BadRequest(ResponseDto<object>.FailResult("NO_FILE", "Vui lòng chọn file để upload."));

        if (!IsExcelFile(file.FileName))
            return BadRequest(ResponseDto<object>.FailResult("INVALID_FILE_TYPE", "Chỉ chấp nhận file .xlsx"));

        await using var stream = file.OpenReadStream();

        var command = new ImportQuestionsCommand(
            quizMaterialId,
            identity.UserId,
            stream,
            DryRun: false);

        var result = await _sender.Send(command, ct);

        if (!result.Success)
        {
            return result.ErrorCode switch
            {
                "QUIZ_NOT_FOUND"  => NotFound(result),
                "FORBIDDEN"       => StatusCode(StatusCodes.Status403Forbidden, result),
                "INVALID_FILE"    => BadRequest(result),
                "NO_VALID_ROWS"   => BadRequest(result),
                _                 => BadRequest(result)
            };
        }

        return Ok(result);
    }

    private static bool IsExcelFile(string fileName)
        => fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase);
}
