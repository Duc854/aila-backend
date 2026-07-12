using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Tags.Dtos;
using MediatR;
using Shared.Wrappers;
using System.Threading;


namespace AILA.Application.Features.Tags.Commands.UpdateSystemTag
{
    public class UpdateSystemTagCommandHandler(IUnitOfWork uow)
        : IRequestHandler<UpdateSystemTagCommand, ResponseDto<TagDto>>
    {
        public async Task<ResponseDto<TagDto>> Handle(
            UpdateSystemTagCommand request,
            CancellationToken ct)
        {
            var tag = await uow.Tags.GetByIdAsync(request.TagId);

            if (tag == null)
            {
                return ResponseDto<TagDto>.FailResult(
                    "NOT_FOUND",
                    "Không tìm thấy tag.");
            }

            if (tag.CreatedById != null)
            {
                return ResponseDto<TagDto>.FailResult(
                    "CUSTOM_TAG_NOT_UPDATABLE",
                    "Chỉ System Tag mới được cập nhật.");
            }

            var normalizedName = request.Name.Trim();
            var code = normalizedName.ToLower().Replace(" ", "-");

            if (await uow.Tags.CodeExistsAsync(code, ct) &&
                !string.Equals(tag.Code, code, StringComparison.OrdinalIgnoreCase))
            {
                return ResponseDto<TagDto>.FailResult(
                    "DUPLICATE_TAG",
                    "Tag đã tồn tại.");
            }

            if (await uow.Tags.IsAssignedToCourseAsync(tag.Id, ct))
            {
                return ResponseDto<TagDto>.FailResult(
                    "TAG_IN_USE",
                    "Tag đang được sử dụng không thể cập nhật.");
            }

            tag.UpdateSystemTag(normalizedName, code);
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