using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Tags.Dtos;
using AILA.Domain.Entities;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Tags.Commands.CreateSystemTag
{
    public class CreateSystemTagCommandHandler(IUnitOfWork uow)
        : IRequestHandler<CreateSystemTagCommand, ResponseDto<TagDto>>
    {
        public async Task<ResponseDto<TagDto>> Handle(
            CreateSystemTagCommand request,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.Name) ||
                request.Name.Trim().Length < 2 ||
                request.Name.Trim().Length > 100)
            {
                return ResponseDto<TagDto>.FailResult(
                    "INVALID_NAME",
                    "Tên tag phải từ 2 đến 100 ký tự.");
            }

            var normalizedName = request.Name.Trim();
            var code = normalizedName.ToLower().Replace(" ", "-");

            // BR-01
            if (await uow.Tags.CodeExistsAsync(code, ct))
            {
                return ResponseDto<TagDto>.FailResult(
                    "DUPLICATE_TAG",
                    "Tag đã tồn tại.");
            }

            var tag = Tag.CreateByAdmin(normalizedName, code);

            await uow.Tags.AddAsync(tag);
            await uow.SaveChangesAsync(ct);

            return ResponseDto<TagDto>.SuccessResult(new TagDto
            {
                Id = tag.Id,
                Name = tag.Name,
                Code = tag.Code,
                IsPublished = tag.IsPublished,
                CreatedById = tag.CreatedById,
                Source = "Admin",
                UsageCount = 0
            });
        }
    }
}
