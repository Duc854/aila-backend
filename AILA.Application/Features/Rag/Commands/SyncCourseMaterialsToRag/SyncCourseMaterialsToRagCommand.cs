using AILA.Application.Common.Dtos.Rag;
using MediatR;
using System;

namespace AILA.Application.Features.Rag.Commands.SyncCourseMaterialsToRag;

public record SyncCourseMaterialsToRagCommand(
    Guid CourseId
) : IRequest<SyncCourseRagResponseDto>;