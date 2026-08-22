using AILA.Application.Common.Dtos.Rag;
using MediatR;
using System;

namespace AILA.Application.Features.Rag.Commands.CreateCourseChatSession;

public record CreateCourseChatSessionCommand(
    Guid AccountId,
    Guid CourseId,
    string Title
) : IRequest<CourseChatSessionDto>;