using AILA.Application.Common.Interfaces;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Categories.Commands.ReorderCategories
{
    /// <summary>
    /// UC-84 - Reorder Course Categories
    /// </summary>
    public class ReorderCategoriesCommandHandler(IUnitOfWork uow)
        : IRequestHandler<ReorderCategoriesCommand, ResponseDto<object>>
    {
        public async Task<ResponseDto<object>> Handle(
            ReorderCategoriesCommand request,
            CancellationToken ct)
        {
            // BR-03
            if (request.CategoryIds == null || request.CategoryIds.Count == 0)
            {
                return ResponseDto<object>.FailResult(
                    "INVALID_ORDER",
                    "Danh sách sắp xếp không hợp lệ.");
            }

            if (request.CategoryIds.Any(id => id == Guid.Empty))
            {
                return ResponseDto<object>.FailResult(
                    "INVALID_ORDER",
                    "Danh sách sắp xếp không hợp lệ.");
            }

            if (request.CategoryIds.Count != request.CategoryIds.Distinct().Count())
            {
                return ResponseDto<object>.FailResult(
                    "INVALID_ORDER",
                    "Danh sách sắp xếp không hợp lệ.");
            }

            // Lấy toàn bộ category theo ID (tracked by EF Core)
            var allCategories = await uow.Categories
                .GetByIdsAsync(request.CategoryIds, ct);

            var totalCount = (await uow.Categories.GetAllOrderedAsync(ct)).Count();

            // BR-03:
            // Danh sách submit phải chứa tất cả category đúng một lần
            if (allCategories.Count != totalCount || allCategories.Count != request.CategoryIds.Count)
            {
                return ResponseDto<object>.FailResult(
                    "INVALID_ORDER",
                    "Danh sách sắp xếp không hợp lệ.");
            }

            var dbIds = allCategories
                .Select(c => c.Id)
                .OrderBy(x => x);

            var requestIds = request.CategoryIds
                .OrderBy(x => x);

            if (!dbIds.SequenceEqual(requestIds))
            {
                return ResponseDto<object>.FailResult(
                    "INVALID_ORDER",
                    "Danh sách sắp xếp không hợp lệ.");
            }

            // BR-02: Cập nhật thứ tự hiển thị (1-based: 1, 2, 3...)
            for (int i = 0; i < request.CategoryIds.Count; i++)
            {
                var category = allCategories
                    .First(c => c.Id == request.CategoryIds[i]);

                category.ChangeOrder(i + 1);
                uow.Categories.Update(category);
            }

            await uow.SaveChangesAsync(ct);

            return ResponseDto<object>.SuccessResult(new
            {
                Message = "Category order updated successfully."
            });
        }
    }
}
