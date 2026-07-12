namespace AILA.Api.Controllers.Admin
{
    public record ManageCategoryRequest(string Name, string? Description, int OrderIndex, bool IsActive);

    public record ChangeCategoryStatusRequest(bool IsActive);

    public record ReorderCategoriesRequest(List<Guid> CategoryIds);
}
