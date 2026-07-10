using AILA.Application.Common.Interfaces;
using AILA.Domain.Entities;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.QuizMaterials.Commands.BulkCreateQuiz;

public sealed class BulkCreateQuizCommandHandler
    : IRequestHandler<
        BulkCreateQuizCommand,
        ResponseDto<object>>
{
    private readonly IUnitOfWork _uow;

    public BulkCreateQuizCommandHandler(
        IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ResponseDto<object>> Handle(
        BulkCreateQuizCommand request,
        CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}