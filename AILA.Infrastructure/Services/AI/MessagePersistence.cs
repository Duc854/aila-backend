using AILA.Application.Common.Dtos.Rag;
using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.AI;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Infrastructure.Services.AI;

public class MessagePersistence : IMessagePersistence
{
    private readonly ICourseChatMessageRepository _messageRepository;
    private readonly IUnitOfWork _unitOfWork; // ✅ Chỉ cần UnitOfWork

    public MessagePersistence(
        ICourseChatMessageRepository messageRepository,
        IUnitOfWork unitOfWork) // ✅ Không cần thêm dependency nào khác
    {
        _messageRepository = messageRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task SaveViolationRecordAsync(
        Guid accountId,
        string violationType,
        string policyName,
        string reason,
        string prompt,
        CancellationToken cancellationToken = default)
    {
        var record = new UserViolationRecord(
            accountId,
            violationType,
            policyName,
            reason,
            prompt);

        // ✅ Dùng UnitOfWork, không cần interface mới
        await _unitOfWork.Repository<UserViolationRecord>().AddAsync(record);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveMessagesAsync(
        Guid sessionId,
        string question,
        string answer,
        List<RagCitationDto> citations,
        int promptTokens,
        int completionTokens,
        CancellationToken cancellationToken = default)
    {
        var userMsg = new CourseChatMessage(sessionId, "user", question, null, 0, 0);
        var citationsJson = JsonSerializer.Serialize(citations);
        var aiMsg = new CourseChatMessage(sessionId, "assistant", answer, citationsJson, promptTokens, completionTokens);

        await _messageRepository.AddMessageAsync(userMsg, cancellationToken);
        await _messageRepository.AddMessageAsync(aiMsg, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}