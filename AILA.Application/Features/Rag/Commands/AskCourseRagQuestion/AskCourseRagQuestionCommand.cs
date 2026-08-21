using AILA.Application.Common.Dtos.Rag;
using MediatR;
using System;

namespace AILA.Application.Features.Rag.Commands.AskCourseRagQuestion;

public record AskCourseRagQuestionCommand(
    Guid SessionId,
    Guid AccountId,
    string Question
) : IRequest<AskRagQuestionResponseDto>;