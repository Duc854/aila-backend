using AILA.Application.Common.Interfaces;
using MediatR;

namespace AILA.Application.Features.Tags.Queries
{
    public class CheckTagCodeQueryHandler : IRequestHandler<CheckTagCodeQuery, bool>
    {
        private readonly IUnitOfWork _uow;

        public CheckTagCodeQueryHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<bool> Handle(
            CheckTagCodeQuery request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Code))
                return false;

            var normalizedCode = request.Code.ToLower().Trim().Replace(" ", "-");
            return await _uow.Tags.CodeExistsAsync(normalizedCode, cancellationToken);
        }
    }
}
