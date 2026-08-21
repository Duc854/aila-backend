using AILA.Application.Common.Dtos.Rag;
using MediatR;
using System;
using System.Collections.Generic;

namespace AILA.Application.Features.Rag.Queries.GetCourseChatMessages;

public record GetCourseChatMessagesQuery(
    Guid SessionId,
    Guid AccountId
) : IRequest<List<CourseChatMessageDto>>;