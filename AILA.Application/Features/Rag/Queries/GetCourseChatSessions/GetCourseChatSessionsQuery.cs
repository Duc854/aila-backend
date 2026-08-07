using AILA.Application.Common.Dtos.Rag;
using AILA.Application.Common.Interfaces.Repositories;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Features.Rag.Queries.GetCourseChatSessions;

public record GetCourseChatSessionsQuery(Guid AccountId, Guid CourseId) : IRequest<List<CourseChatSessionDto>>;

public class GetCourseChatSessionsQueryHandler : IRequestHandler<GetCourseChatSessionsQuery, List<CourseChatSessionDto>>
{
    private readonly IKnowledgeChunkRepository _repository;

    public GetCourseChatSessionsQueryHandler(IKnowledgeChunkRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<CourseChatSessionDto>> Handle(GetCourseChatSessionsQuery request, CancellationToken cancellationToken)
    {
        var sessions = await _repository.GetSessionsByAccountAndCourseAsync(request.AccountId, request.CourseId, cancellationToken);
        return sessions.Select(s => new CourseChatSessionDto
        {
            Id = s.Id,
            AccountId = s.AccountId,
            CourseId = s.CourseId,
            Title = s.Title,
            CreatedAt = s.CreatedAt
        }).ToList();
    }
}
