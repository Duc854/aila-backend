using AILA.Application.Common.Dtos.Rag;
using MediatR;
using System;
using System.Collections.Generic;

namespace AILA.Application.Features.Rag.Queries.GetCourseChatSessions;

public record GetCourseChatSessionsQuery(
    Guid AccountId,
    Guid CourseId
) : IRequest<List<CourseChatSessionDto>>;