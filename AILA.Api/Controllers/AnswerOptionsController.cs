using AILA.Api.Extensions;
using AILA.Application.Features.AnswerOptions.Commands.CreateAnswerOption;
using AILA.Application.Features.AnswerOptions.Commands.DeleteAnswerOption;
using AILA.Application.Features.AnswerOptions.Commands.ReorderAnswerOptions;
using AILA.Application.Features.AnswerOptions.Commands.UpdateAnswerOption;
using AILA.Application.Features.AnswerOptions.Dtos;
using AILA.Application.Features.AnswerOptions.Queries.GetAnswerOptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Wrappers;

namespace AILA.Api.Controllers;

[Authorize(Roles = "Expert")]
[ApiController]
[Route("api/questions/{questionId:guid}/answer-options")]
public class AnswerOptionsController : ControllerBase
{
    private readonly ISender _sender;

    public AnswerOptionsController(
        ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Lấy danh sách đáp án của câu hỏi.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAnswerOptions(
        Guid questionId,
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

        var query = new GetAnswerOptionsQuery(
            questionId,
            identity.UserId);

        var result = await _sender.Send(query, ct);

        if (!result.Success)
        {
            return result.ErrorCode switch
            {
                "QUESTION_NOT_FOUND" => NotFound(result),
                "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, result),
                _ => BadRequest(result)
            };
        }

        return Ok(result);
    }

    /// <summary>
    /// Tạo đáp án mới.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateAnswerOption(
        Guid questionId,
        [FromBody] SaveAnswerOptionRequest request,
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

        var command = new CreateAnswerOptionCommand(
            questionId,
            identity.UserId,
            request.Content,
            request.IsCorrect);

        var result = await _sender.Send(command, ct);

        if (!result.Success)
        {
            return result.ErrorCode switch
            {
                "QUESTION_NOT_FOUND" => NotFound(result),
                "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, result),
                _ => BadRequest(result)
            };
        }

        return Ok(result);
    }

    /// <summary>
    /// Cập nhật đáp án.
    /// </summary>
    [HttpPut("{answerOptionId:guid}")]
    public async Task<IActionResult> UpdateAnswerOption(
        Guid questionId,
        Guid answerOptionId,
        [FromBody] SaveAnswerOptionRequest request,
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

        var command = new UpdateAnswerOptionCommand(
            answerOptionId,
            identity.UserId,
            request.Content,
            request.IsCorrect);

        var result = await _sender.Send(command, ct);

        if (!result.Success)
        {
            return result.ErrorCode switch
            {
                "ANSWER_NOT_FOUND" => NotFound(result),
                "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, result),
                _ => BadRequest(result)
            };
        }

        return Ok(result);
    }

    /// <summary>
    /// Xóa đáp án.
    /// </summary>
    [HttpDelete("{answerOptionId:guid}")]
    public async Task<IActionResult> DeleteAnswerOption(
        Guid questionId,
        Guid answerOptionId,
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

        var command = new DeleteAnswerOptionCommand(
            answerOptionId,
            identity.UserId);

        var result = await _sender.Send(command, ct);

        if (!result.Success)
        {
            return result.ErrorCode switch
            {
                "ANSWER_NOT_FOUND" => NotFound(result),
                "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, result),
                _ => BadRequest(result)
            };
        }

        return NoContent();
    }

    /// <summary>
    /// Sắp xếp lại thứ tự đáp án.
    /// </summary>
    [HttpPut("reorder")]
    public async Task<IActionResult> ReorderAnswerOptions(
        Guid questionId,
        [FromBody] ReorderAnswerOptionsRequest request,
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

        var command = new ReorderAnswerOptionsCommand(
            questionId,
            identity.UserId,
            request.Items);

        var result = await _sender.Send(command, ct);

        if (!result.Success)
        {
            return result.ErrorCode switch
            {
                "QUESTION_NOT_FOUND" => NotFound(result),
                "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, result),
                _ => BadRequest(result)
            };
        }

        return Ok(result);
    }
}