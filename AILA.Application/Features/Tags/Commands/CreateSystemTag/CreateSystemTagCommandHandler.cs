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
        : IRequestHandler<CreateSystemTagCommand, ResponseDto<SystemTagDto>>
    {
        public async Task<ResponseDto<SystemTagDto>> Handle(
            CreateSystemTagCommand request,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.Name) ||
                request.Name.Trim().Length < 2 ||
                request.Name.Trim().Length > 100)
            {
                return ResponseDto<SystemTagDto>.FailResult(
                    "INVALID_NAME",
                    "Tên tag phải từ 2 đến 100 ký tự.");
            }

            var normalizedName = request.Name.Trim();

            var code = normalizedName
                .ToLower()
                .Replace(" ", "-");

            // BR-01:
            // Nếu custom tag cùng code tồn tại thì reuse và convert thành system tag
            var existingTag = await uow.Tags.GetByCodeAsync(code, ct);

            if (existingTag != null)
            {
                // System tag đã tồn tại
                if (existingTag.CreatedById == null)
                {
                    return ResponseDto<SystemTagDto>.FailResult(
                        "DUPLICATE_TAG",
                        "Tag hệ thống này đã tồn tại.");
                }

                // Custom tag -> System tag
                existingTag.ConvertToSystemTag();

                uow.Tags.Update(existingTag);
                await uow.SaveChangesAsync(ct);

                return ResponseDto<SystemTagDto>.SuccessResult(new SystemTagDto
                {
                    Id = existingTag.Id,
                    Name = existingTag.Name,
                    Code = existingTag.Code,
                    IsPublished = existingTag.IsPublished,
                    Source = "System",
                    UsageCount = 0,
                    CreatedAt = existingTag.CreatedAt
                });
            }


            // Create new system tag
            var tag = Tag.CreateByAdmin(
                normalizedName,
                code
            );

            await uow.Tags.AddAsync(tag);
            await uow.SaveChangesAsync(ct);


            return ResponseDto<SystemTagDto>.SuccessResult(new SystemTagDto
            {
                Id = tag.Id,
                Name = tag.Name,
                Code = tag.Code,
                IsPublished = tag.IsPublished,
                Source = "System",
                UsageCount = 0,
                CreatedAt = tag.CreatedAt
            });
        }
    }
}