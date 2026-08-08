using AILA.Api.Extensions;
using AILA.Application.Features.Questions.Commands.CreateQuestion;
using AILA.Application.Features.Questions.Commands.DeleteQuestion;
using AILA.Application.Features.Questions.Commands.ReorderQuestions;
using AILA.Application.Features.Questions.Commands.UpdateQuestion;
using AILA.Application.Features.Questions.Dtos;
using AILA.Application.Features.Questions.Queries.GetQuestions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Wrappers;

namespace AILA.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/quiz-materials/{quizMaterialId:guid}/questions")]
public class QuestionsController : ControllerBase{
    private readonly ISender _sender;

    public QuestionsController(ISender sender)
    {
        _sender = sender;
    }
    [HttpGet]
    [Authorize(Roles = "Expert,Admin")]
    public async Task<IActionResult> GetQuestions(
    Guid quizMaterialId,
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

        var query = new GetQuestionsQuery(
            quizMaterialId,
            identity.UserId,
            IsAdminOverride: isAdmin);

        var result = await _sender.Send(query, ct);

        if (!result.Success)
        {
            return result.ErrorCode switch
            {
                "QUIZ_NOT_FOUND" => NotFound(result),
                "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, result),
                _ => BadRequest(result)
            };
        }

        return Ok(result);
    }
    [HttpPost]
    [Authorize(Roles = "Expert")]
    public async Task<IActionResult> CreateQuestion(
    Guid quizMaterialId,
    [FromBody] SaveQuestionRequest request,
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

        var command = new CreateQuestionCommand(
            quizMaterialId,
            identity.UserId,
            request.Content,
            request.QuestionType);

        var result = await _sender.Send(command, ct);

        if (!result.Success)
        {
            return result.ErrorCode switch
            {
                "QUIZ_NOT_FOUND" => NotFound(result),
                "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, result),
                _ => BadRequest(result)
            };
        }

        return Ok(result);
    }
    [HttpPut("{questionId:guid}")]
    [Authorize(Roles = "Expert")]
    public async Task<IActionResult> UpdateQuestion(
    Guid quizMaterialId,
    Guid questionId,
    [FromBody] SaveQuestionRequest request,
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

        var command = new UpdateQuestionCommand(
            questionId,
            identity.UserId,
            request.Content,
            request.QuestionType);

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
    [HttpDelete("{questionId:guid}")]
    [Authorize(Roles = "Expert")]
    public async Task<IActionResult> DeleteQuestion(
    Guid quizMaterialId,
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

        var command = new DeleteQuestionCommand(
            questionId,
            identity.UserId);

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

        return NoContent();
    }
    [HttpPut("reorder")]
    [Authorize(Roles = "Expert")]
    public async Task<IActionResult> ReorderQuestions(
    Guid quizMaterialId,
    [FromBody] ReorderQuestionsRequest request,
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

        var command = new ReorderQuestionsCommand(
            quizMaterialId,
            identity.UserId,
            request.Items);

        var result = await _sender.Send(command, ct);

        if (!result.Success)
        {
            return result.ErrorCode switch
            {
                "QUIZ_NOT_FOUND" => NotFound(result),
                "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, result),
                _ => BadRequest(result)
            };
        }

        return Ok(result);
    }
}