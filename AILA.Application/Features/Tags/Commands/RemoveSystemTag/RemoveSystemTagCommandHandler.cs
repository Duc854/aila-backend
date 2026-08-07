using AILA.Application.Common.Interfaces;
using AILA.Domain.Constants;
using AILA.Domain.Entities;
using MediatR;
using Shared.Wrappers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Features.Tags.Commands.RemoveSystemTag
{
    public class RemoveSystemTagCommandHandler
        : IRequestHandler<RemoveSystemTagCommand, ResponseDto<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public RemoveSystemTagCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ResponseDto<bool>> Handle(
            RemoveSystemTagCommand request,
            CancellationToken cancellationToken)
        {
            // Validate request
            if (request.TagId == Guid.Empty)
            {
                return ResponseDto<bool>.FailResult(
                    "INVALID_TAG_ID",
                    "Tag ID không hợp lệ.");
            }

            var tagRepository = _unitOfWork.Repository<Tag>();
            var tag = await tagRepository.GetByIdAsync(request.TagId);

            if (tag == null)
            {
                return ResponseDto<bool>.FailResult(
                    "TAG_NOT_FOUND",
                    $"Không tìm thấy tag với ID: {request.TagId}");
            }

            // Reserved tags created by system seeding cannot be removed
            if (ReservedTagCodes.All.Contains(tag.Code))
            {
                return ResponseDto<bool>.FailResult(
                    "RESERVED_TAG",
                    $"Không thể xóa tag được tạo tự động bởi hệ thống '{tag.Name}'.");
            }

            // System Tag trong nghiệp vụ AILA = Tag do Admin tạo
            // Expert Custom Tag không được phép xóa bằng flow này
            if (tag.CreatedById != null)
            {
                return ResponseDto<bool>.FailResult(
                    "NOT_SYSTEM_TAG",
                    $"Tag '{tag.Name}' được tạo bởi chuyên gia. Chỉ có thể xóa System Tag.");
            }

            var isTagInUse = await _unitOfWork.Tags.IsAssignedToCourseAsync(tag.Id,cancellationToken);
            if (isTagInUse)
            {
                return ResponseDto<bool>.FailResult(
                    "TAG_IN_USE",
                    "Tag đang được sử dụng trong khóa học, không thể xóa.");
            }


            tagRepository.Delete(tag);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ResponseDto<bool>.SuccessResult(true);
        }
    }
}
