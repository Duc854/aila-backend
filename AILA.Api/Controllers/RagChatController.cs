using AILA.Api.Extensions;
using AILA.Application.Common.Dtos.Rag;
using AILA.Application.Features.Rag.Commands.AskCourseRagQuestion;
using AILA.Application.Features.Rag.Commands.CreateCourseChatSession;
using AILA.Application.Features.Rag.Commands.IndexDocumentMaterial;
using AILA.Application.Features.Rag.Queries.GetCourseChatMessages;
using AILA.Application.Features.Rag.Queries.GetCourseChatSessions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Wrappers;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Api.Controllers;

[ApiController]
[Route("api/rag")]
public class RagChatController : ControllerBase
{
    private readonly IMediator _mediator;

    public RagChatController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Admin/Expert index nội dung văn bản bài học (DocumentMaterial) vào kho tri thức RAG.
    /// </summary>
    [HttpPost("documents/{materialId:guid}/index")]
    [Authorize(Roles = "Admin,Expert")]
    public async Task<ActionResult<IndexDocumentResponseDto>> IndexDocumentMaterial(
        Guid materialId,
        [FromBody] IndexDocumentRequest request,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new IndexDocumentMaterialCommand(
            materialId,
            request.CourseId,
            request.MaterialTitle,
            request.ContentText), ct);

        return Ok(result);
    }

    /// <summary>
    /// UC-31 Step 2: Lấy session hiện có hoặc tạo mới cho learner + course.
    /// Đảm bảo BR-01: mỗi learner chỉ có 1 session per course.
    /// </summary>
    [HttpPost("sessions")]
    [Authorize(Roles = "Learner")]
    public async Task<ActionResult<CourseChatSessionDto>> GetOrCreateChatSession(
        [FromBody] CreateSessionRequest request,
        CancellationToken ct)
    {
        var identity = HttpContext.GetUserIdentity();
        if (identity is null)
            return Unauthorized(ResponseDto<object>.FailResult("UNAUTHORIZED", "Xác thực thất bại."));

        // BR-01: kiểm tra session đã tồn tại chưa
        var existing = await _mediator.Send(
            new GetCourseChatSessionsQuery(identity.UserId, request.CourseId), ct);

        if (existing.Count > 0)
            return Ok(existing[0]); // trả session cũ nhất (hoặc duy nhất)

        var result = await _mediator.Send(new CreateCourseChatSessionCommand(
            identity.UserId,
            request.CourseId,
            request.Title), ct);

        return Ok(result);
    }

    /// <summary>
    /// Lấy lịch sử tất cả tin nhắn hỏi đáp trong 1 phiên chat RAG.
    /// </summary>
    [HttpGet("sessions/{sessionId:guid}/messages")]
    [Authorize(Roles = "Learner")]
    public async Task<ActionResult<List<CourseChatMessageDto>>> GetChatMessages(
        Guid sessionId,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCourseChatMessagesQuery(sessionId), ct);
        return Ok(result);
    }

    /// <summary>
    /// UC-31 Step 3-7: Học viên gửi câu hỏi, hệ thống trả lời dùng RAG + LLM.
    /// </summary>
    [HttpPost("sessions/{sessionId:guid}/ask")]
    [Authorize(Roles = "Learner")]
    public async Task<ActionResult<AskRagQuestionResponseDto>> AskQuestion(
        Guid sessionId,
        [FromBody] AskQuestionRequest request,
        CancellationToken ct)
    {
        var identity = HttpContext.GetUserIdentity();
        if (identity is null)
            return Unauthorized(ResponseDto<object>.FailResult("UNAUTHORIZED", "Xác thực thất bại."));

        var result = await _mediator.Send(
            new AskCourseRagQuestionCommand(sessionId, identity.UserId, request.Question), ct);

        return Ok(result);
    }
}

public class IndexDocumentRequest
{
    public Guid CourseId { get; set; }
    public string MaterialTitle { get; set; } = string.Empty;
    public string ContentText { get; set; } = string.Empty;
}

public class CreateSessionRequest
{
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
}

public class AskQuestionRequest
{
    public string Question { get; set; } = string.Empty;
}
