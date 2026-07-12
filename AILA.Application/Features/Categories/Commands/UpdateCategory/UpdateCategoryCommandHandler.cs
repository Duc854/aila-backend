using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Categories.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Categories.Commands.UpdateCategory
{
    /// <summary>
    /// UC-82 - Update Course Category
    /// </summary>
    public class UpdateCategoryCommandHandler(IUnitOfWork uow)
        : IRequestHandler<UpdateCategoryCommand, ResponseDto<CategoryDto>>
    {
        public async Task<ResponseDto<CategoryDto>> Handle(
            UpdateCategoryCommand request,
            CancellationToken ct)
        {
            var category = await uow.Categories.GetByIdAsync(request.CategoryId);

            if (category == null)
            {
                return ResponseDto<CategoryDto>.FailResult(
                    "CATEGORY_NOT_FOUND",
                    "Không tìm thấy danh mục.");
            }

            var name = request.Name?.Trim();

            // BR-02
            if (string.IsNullOrWhiteSpace(name))
            {
                return ResponseDto<CategoryDto>.FailResult(
                    "CATEGORY_NAME_REQUIRED",
                    "Tên danh mục là bắt buộc.");
            }

            if (name.Length < 2 || name.Length > 100)
            {
                return ResponseDto<CategoryDto>.FailResult(
                    "INVALID_CATEGORY_NAME",
                    "Tên danh mục phải từ 2 đến 100 ký tự.");
            }

            // BR-01
            if (await uow.Categories.ExistsByNameExceptIdAsync(
                request.CategoryId,
                name,
                ct))
            {
                return ResponseDto<CategoryDto>.FailResult(
                    "CATEGORY_ALREADY_EXISTS",
                    "Tên danh mục đã tồn tại.");
            }

            // BR-03
            category.UpdateInfo(
                name,
                request.Description);

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