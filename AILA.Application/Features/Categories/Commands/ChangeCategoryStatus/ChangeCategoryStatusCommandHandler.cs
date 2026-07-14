using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Categories.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Categories.Commands.ChangeCategoryStatus
{
    /// <summary>
    /// UC-83 - Change Course Category Status
    /// </summary>
    public class ChangeCategoryStatusCommandHandler(IUnitOfWork uow)
        : IRequestHandler<ChangeCategoryStatusCommand, ResponseDto<CategoryDto>>
    {
        public async Task<ResponseDto<CategoryDto>> Handle(
            ChangeCategoryStatusCommand request,
            CancellationToken ct)
        {
            var category = await uow.Categories.GetByIdAsync(request.CategoryId);

            if (category == null)
            {
                return ResponseDto<CategoryDto>.FailResult(
                    "CATEGORY_NOT_FOUND",
                    "Không tìm thấy danh mục.");
            }

            // BR-01
            if (!request.IsActive)
            {
                var hasCourses = await uow.Categories.HasCoursesAsync(
                    request.CategoryId,
                    ct);

                if (hasCourses)
                {
                    return ResponseDto<CategoryDto>.FailResult(
                        "CATEGORY_HAS_COURSES",
                        "Không thể vô hiệu hóa danh mục đang được sử dụng.");
                }
            }

            // BR-02
            if (request.IsActive)
            {
                category.Activate();
            }
            else
            {
                category.Deactivate();
            }

            uow.Categories.Update(category);

            await uow.SaveChangesAsync(ct);

            var dto = new CategoryDto(
                category.Id,
                category.Name,
                category.Description,
                category.OrderIndex,
                category.IsActive);

            return ResponseDto<CategoryDto>.SuccessResult(dto);
        }
    }
}