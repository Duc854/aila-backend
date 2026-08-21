using AILA.Application.Common.Dtos.Rag;
using AILA.Application.Common.Interfaces.AI;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Features.Rag.Commands.AskCourseRagQuestion;

public class AskCourseRagQuestionCommandHandler
    : IRequestHandler<AskCourseRagQuestionCommand, AskRagQuestionResponseDto>
{
    private readonly IRagChatService _ragChatService;

    public AskCourseRagQuestionCommandHandler(
        IRagChatService ragChatService)
    {
        _ragChatService = ragChatService;
    }

    public async Task<AskRagQuestionResponseDto> Handle(
        AskCourseRagQuestionCommand request,
        CancellationToken cancellationToken)
    {
        return await _ragChatService.AskCourseQuestionAsync(
            request.SessionId,
            request.AccountId,
            request.Question,
            cancellationToken);
    }
}