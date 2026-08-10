using AILA.Api.Extensions;
using AILA.Application.Features.QuizMaterials.Commands.BulkCreateQuiz;
using AILA.Application.Features.QuizMaterials.Commands.UpdateQuizDetail;
using AILA.Application.Features.QuizMaterials.Dtos;
using AILA.Application.Features.QuizMaterials.Dtos.BulkCreateQuiz;
using AILA.Application.Features.QuizMaterials.Queries.GetQuizDetail;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Wrappers;

namespace AILA.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/quiz-materials")]
public class QuizMaterialsController : ControllerBase{
    private readonly ISender _sender;

    public QuizMaterialsController(
        ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Lấy thông tin chi tiết Quiz Material.
    /// </summary>
    [HttpGet("{materialId:guid}")]
    [Authorize(Roles = "Expert,Admin")]
    public async Task<IActionResult> GetQuizDetail(
        Guid materialId,
        CancellationToken ct)
    {
        var identity = HttpContext.GetUserIdentity();

        if (identity == null)
        {
            return Unauthorized(
                ResponseDto<object>.FailResult(
                    "AUTH_FAILED",
                    "Xác thực thất bại."));
        }

        var isAdmin = identity.Role == "Admin";

        var query = new GetQuizDetailQuery(
            materialId,
            identity.UserId,
            IsAdminOverride: isAdmin);

        var result = await _sender.Send(query, ct);

        if (!result.Success)
        {
            return result.ErrorCode switch
            {
                "MATERIAL_NOT_FOUND" => NotFound(result),
                "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, result),
                _ => BadRequest(result)
            };
        }

        return Ok(result);
    }

    /// <summary>
    /// Cập nhật thông tin Quiz Material.
    /// </summary>
    [HttpPut("{materialId:guid}")]
    [Authorize(Roles = "Expert")]
    public async Task<IActionResult> UpdateQuizDetail(
        Guid materialId,
        [FromBody] UpdateQuizDetailRequest request,
        CancellationToken ct)
    {
        var identity = HttpContext.GetUserIdentity();

        if (identity == null)
        {
            return Unauthorized(
                ResponseDto<object>.FailResult(
                    "AUTH_FAILED",
                    "Xác thực thất bại."));
        }

        var command = new UpdateQuizDetailCommand(
            materialId,
            identity.UserId,
            request.TimeLimitMinutes,
            request.PassingScore,
            request.ShowCorrectAnswersAfterSubmission);

        var result = await _sender.Send(command, ct);

        if (!result.Success)
        {
            return result.ErrorCode switch
            {
                "MATERIAL_NOT_FOUND" => NotFound(result),
                "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, result),
                _ => BadRequest(result)
            };
        }

        return Ok(result);
    }

    /// <summary>
    /// Tạo nhanh toàn bộ Quiz (Question + AnswerOption) trong một Transaction.
    /// </summary>
    [HttpPost("{materialId:guid}/bulk")]
    [Authorize(Roles = "Expert")]
    public async Task<IActionResult> BulkCreateQuiz(
        Guid materialId,
        [FromBody] BulkCreateQuizRequest request,
        CancellationToken ct)
    {
        var identity = HttpContext.GetUserIdentity();

        if (identity == null)
        {
            return Unauthorized(
                ResponseDto<object>.FailResult(
                    "AUTH_FAILED",
                    "Xác thực thất bại."));
        }

        var command = new BulkCreateQuizCommand(
            materialId,
            identity.UserId,
            request.TimeLimitMinutes,
            request.PassingScore,
            request.ShowCorrectAnswersAfterSubmission,
            request.Questions);

        var result = await _sender.Send(command, ct);

        if (!result.Success)
        {
            return result.ErrorCode switch
            {
                "MATERIAL_NOT_FOUND"
                    => NotFound(result),

                "FORBIDDEN"
                    => StatusCode(
                        StatusCodes.Status403Forbidden,
                        result),

                "INVALID_TYPE"
                    => BadRequest(result),

                "INVALID_QUESTION"
                    => BadRequest(result),

                _ => BadRequest(result)
            };
        }

        return Ok(result);
    }
}