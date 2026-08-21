using AILA.Application.Common.Dtos.Rag;
using AILA.Application.Common.Exceptions;
using AILA.Application.Common.Interfaces.Repositories;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Features.Rag.Queries.GetCourseChatMessages;

public class GetCourseChatMessagesQueryHandler
    : IRequestHandler<GetCourseChatMessagesQuery, List<CourseChatMessageDto>>
{
    private readonly ICourseChatSessionRepository _repository;
    private readonly ICourseChatMessageRepository _messageRepository;

    public GetCourseChatMessagesQueryHandler(
        ICourseChatSessionRepository repository,
        ICourseChatMessageRepository messageRepository)
    {
        _repository = repository;
        _messageRepository = messageRepository;
    }

    public async Task<List<CourseChatMessageDto>> Handle(
        GetCourseChatMessagesQuery request,
        CancellationToken cancellationToken)
    {
        var session = await _repository.GetSessionByIdAsync(
            request.SessionId,
            cancellationToken)
            ?? throw new NotFoundException(
                "Phiên trò chuyện không tồn tại",
                request.SessionId);

        if (session.AccountId != request.AccountId)
        {
            throw new ForbiddenAccessException(
                "Bạn không có quyền xem tin nhắn của phiên trò chuyện này.");
        }

        var messages = await _messageRepository.GetMessagesBySessionIdAsync(
            request.SessionId,
            cancellationToken);

        return messages.Select(m => new CourseChatMessageDto
        {
            Id = m.Id,
            SessionId = m.SessionId,
            Role = m.Role,
            Content = m.Content,
            Citations = string.IsNullOrWhiteSpace(m.CitationsJson)
                ? new List<RagCitationDto>()
                : JsonSerializer.Deserialize<List<RagCitationDto>>(m.CitationsJson)
                    ?? new List<RagCitationDto>(),
            PromptTokens = m.PromptTokens,
            CompletionTokens = m.CompletionTokens,
            CreatedAt = m.CreatedAt
        }).ToList();
    }
}