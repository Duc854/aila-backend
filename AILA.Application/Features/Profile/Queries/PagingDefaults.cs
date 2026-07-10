using Shared.Wrappers;

namespace AILA.Application.Features.Profile.Queries
{
    /// <summary>
    /// Chuẩn hóa tham số phân trang cho các màn "Xem tất cả" của Learning Profile (UC-30):
    /// PageIndex tối thiểu 1, PageSize kẹp trong [1, MaxPageSize] với mặc định DefaultPageSize.
    /// </summary>
    public static class PagingDefaults
    {
        public const int DefaultPageSize = 10;
        public const int MaxPageSize = 50;

        public static (int PageIndex, int PageSize) Normalize(PageRequest? page)
        {
            var pageIndex = page?.PageIndex ?? 1;
            if (pageIndex < 1) pageIndex = 1;

            var pageSize = page?.PageSize ?? DefaultPageSize;
            if (pageSize < 1) pageSize = DefaultPageSize;
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;

            return (pageIndex, pageSize);
        }
    }
}
