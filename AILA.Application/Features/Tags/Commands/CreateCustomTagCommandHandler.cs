using AILA.Application.Common.Dtos;
using AILA.Application.Common.Interfaces;
using AILA.Domain.Entities;
using MediatR;

namespace AILA.Application.Features.Tags.Commands
{
    public class CreateCustomTagCommandHandler
        : IRequestHandler<CreateCustomTagCommand, ExpertTagDto>
    {
        private readonly IUnitOfWork _uow;

        public CreateCustomTagCommandHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<ExpertTagDto> Handle(
            CreateCustomTagCommand request,
            CancellationToken cancellationToken)
        {
            // 1. Kiểm tra Expert tồn tại
            var expert = await _uow.Experts.GetReadonlyWithUserAsync(request.ExpertId, cancellationToken);
            if (expert == null)
                throw new InvalidOperationException("Chuyên gia không tồn tại.");

            // 2. Chuẩn hóa code và kiểm tra trùng
            var normalizedCode = request.Code.ToLower().Trim().Replace(" ", "-");
            var codeExists = await _uow.Tags.CodeExistsAsync(normalizedCode, cancellationToken);
            if (codeExists)
                throw new InvalidOperationException($"Code tag '{normalizedCode}' đã tồn tại trong hệ thống.");

            // 3. Tạo Tag theo domain factory method
            var tag = Tag.CreateByExpert(request.Name, request.Code, request.ExpertId);

            await _uow.Tags.AddAsync(tag);
            await _uow.SaveChangesAsync(cancellationToken);

            return new ExpertTagDto
            {
                Id          = tag.Id,
                Name        = tag.Name,
                Code        = tag.Code,
                IsPublished = tag.IsPublished,
                CreatedAt   = tag.CreatedAt,
                PublishRequest = null
            };
        }
    }
}
