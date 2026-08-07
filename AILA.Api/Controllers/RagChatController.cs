using AILA.Application.Common.Dtos.Rag;
using AILA.Application.Features.Rag.Commands.AskCourseRagQuestion;
using AILA.Application.Features.Rag.Commands.CreateCourseChatSession;
using AILA.Application.Features.Rag.Commands.IndexDocumentMaterial;
using AILA.Application.Features.Rag.Queries.GetCourseChatMessages;
using AILA.Application.Features.Rag.Queries.GetCourseChatSessions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
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
    /// Admin/Giáo viên index nội dung văn bản bài học (DocumentMaterial) vào kho tri thức RAG
    /// </summary>
    [HttpPost("documents/{materialId:guid}/index")]
    public async Task<ActionResult<IndexDocumentResponseDto>> IndexDocumentMaterial(Guid materialId, [FromBody] IndexDocumentRequest request)
    {
        var result = await _mediator.Send(new IndexDocumentMaterialCommand(
            materialId,
            request.CourseId,
            request.MaterialTitle,
            request.ContentText));

        return Ok(result);
    }

    /// <summary>
    /// Học viên tạo phiên hỏi đáp RAG mới cho một Khóa học
    /// </summary>
    [HttpPost("sessions")]
    public async Task<ActionResult<CourseChatSessionDto>> CreateChatSession([FromBody] CreateSessionRequest request)
    {
        var targetAccountId = request.AccountId ?? Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");
        var result = await _mediator.Send(new CreateCourseChatSessionCommand(
            targetAccountId,
            request.CourseId,
            request.Title));

        return Ok(result);
    }

    /// <summary>
    /// Lấy danh sách các cuộc trò chuyện RAG của Học viên theo Khóa học
    /// </summary>
    [HttpGet("sessions/my-sessions")]
    public async Task<ActionResult<List<CourseChatSessionDto>>> GetMyChatSessions([FromQuery] Guid courseId, [FromQuery] Guid? accountId)
    {
        var targetAccountId = accountId ?? Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");
        var result = await _mediator.Send(new GetCourseChatSessionsQuery(targetAccountId, courseId));
        return Ok(result);
    }

    /// <summary>
    /// Lấy lịch sử tất cả tin nhắn hỏi đáp trong 1 phiên chat RAG
    /// </summary>
    [HttpGet("sessions/{sessionId:guid}/messages")]
    public async Task<ActionResult<List<CourseChatMessageDto>>> GetChatMessages(Guid sessionId)
    {
        var result = await _mediator.Send(new GetCourseChatMessagesQuery(sessionId));
        return Ok(result);
    }

    /// <summary>
    /// Học viên gửi câu hỏi RAG cho Chatbot (Tự động tìm tài liệu bài học + Trích dẫn nguồn + Tính Quota Token)
    /// </summary>
    [HttpPost("sessions/{sessionId:guid}/ask")]
    public async Task<ActionResult<AskRagQuestionResponseDto>> AskQuestion(Guid sessionId, [FromBody] AskQuestionRequest request)
    {
        var targetAccountId = request.AccountId ?? Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");
        var result = await _mediator.Send(new AskCourseRagQuestionCommand(sessionId, targetAccountId, request.Question));
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
    public Guid? AccountId { get; set; }
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
}

public class AskQuestionRequest
{
    public Guid? AccountId { get; set; }
    public string Question { get; set; } = string.Empty;
}
