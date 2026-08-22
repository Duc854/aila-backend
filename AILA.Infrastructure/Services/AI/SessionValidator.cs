using AILA.Application.Common.Dtos.Rag;
using AILA.Application.Common.Interfaces.AI;
using AILA.Application.Common.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Infrastructure.Services.AI;

public class SessionValidator : ISessionValidator
{
    private readonly ICourseChatSessionRepository _sessionRepository;
    private readonly ICourseChatMessageRepository _messageRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;

    public SessionValidator(
        ICourseChatSessionRepository sessionRepository,
        ICourseChatMessageRepository messageRepository,
        IEnrollmentRepository enrollmentRepository)
    {
        _sessionRepository = sessionRepository;
        _messageRepository = messageRepository;
        _enrollmentRepository = enrollmentRepository;
    }

    public async Task<SessionValidationResult> ValidateAsync(
        Guid sessionId,
        Guid accountId,
        string question,
        int maxHistoryMessages = 6,
        CancellationToken cancellationToken = default)
    {
        // Lấy từ RagChatService cũ
        var session = await _sessionRepository.GetSessionByIdAsync(sessionId, cancellationToken);
        if (session == null)
        {
            return SessionValidationResult.Invalid($"Không tìm thấy phiên trò chuyện RAG ID: {sessionId}", "NotFound");
        }

        // Lấy từ RagChatService cũ
        if (session.AccountId != accountId)
        {
            return SessionValidationResult.Invalid("Bạn không có quyền truy cập vào phiên trò chuyện này.", "Forbidden");
        }

        // Lấy từ RagChatService cũ
        var isEnrolled = await _enrollmentRepository.IsLearnerEnrolledInCourseAsync(
            accountId,
            session.CourseId,
            cancellationToken);

        if (!isEnrolled)
        {
            return SessionValidationResult.Invalid("Bạn cần đăng ký khóa học này trước khi sử dụng Trợ lý AI.", "Forbidden");
        }

        // Lấy từ RagChatService cũ
        var recentMessages = await _messageRepository.GetRecentMessagesAsync(
            sessionId,
            maxHistoryMessages,
            cancellationToken);

        // Lấy từ RagChatService cũ
        if (session.Title == "Cuộc trò chuyện mới" || string.IsNullOrWhiteSpace(session.Title))
        {
            var newTitle = question.Length > 50 ? question.Substring(0, 47) + "..." : question;
            session.UpdateTitle(newTitle);
        }

        // Convert Entity -> DTO
        var sessionDto = new CourseChatSessionDto
        {
            Id = session.Id,
            AccountId = session.AccountId,
            CourseId = session.CourseId,
            Title = session.Title
        };

        var messageDtos = recentMessages.Select(m => new CourseChatMessageDto
        {
            Id = m.Id,
            SessionId = m.SessionId,
            Role = m.Role,
            Content = m.Content,
            PromptTokens = m.PromptTokens,
            CompletionTokens = m.CompletionTokens,
            CreatedAt = m.CreatedAt
        }).ToList();

        return SessionValidationResult.Valid(sessionDto, messageDtos);
    }
}