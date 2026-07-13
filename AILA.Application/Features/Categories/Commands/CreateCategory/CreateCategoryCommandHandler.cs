using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Categories.Dtos;
using AILA.Domain.Entities;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Categories.Commands.CreateCategory
{
    /// <summary>
    /// UC-81 - Create Course Category
    /// </summary>
    public class CreateCategoryCommandHandler(IUnitOfWork uow)
        : IRequestHandler<CreateCategoryCommand, ResponseDto<CategoryDto>>
    {
        public async Task<ResponseDto<CategoryDto>> Handle(
            CreateCategoryCommand request,
            CancellationToken ct)
        {
            // BR-02
            var name = request.Name?.Trim();

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

            // BR-03
            if (request.OrderIndex < 0)
            {
                return ResponseDto<CategoryDto>.FailResult(
                    "INVALID_ORDER_INDEX",
                    "Thứ tự hiển thị không hợp lệ.");
            }

            // BR-01
            if (await uow.Categories.ExistsByNameAsync(name, ct))
            {
                return ResponseDto<CategoryDto>.FailResult(
                    "CATEGORY_ALREADY_EXISTS",
                    "Tên danh mục đã tồn tại.");
            }

            // BR-04
            var category = new Category(
                name,
                request.Description,
                request.OrderIndex);

            await uow.Categories.AddAsync(category);

            await uow.SaveChangesAsync(ct);

            var dto = new CategoryDto(
                category.Id,
                category.Name,
                category.Description,
                category.OrderIndex,
                category.IsActive
            );

            return ResponseDto<CategoryDto>.SuccessResult(dto);
        }
    }
}