using AILA.Api.Extensions;
using AILA.Application.Features.Categories.Dtos;
using AILA.Infrastructure.Persistence;
using AILA.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Wrappers;

namespace AILA.Api.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/categories")]
    [Authorize(Roles = "Admin")]
    public class AdminCategoriesController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public AdminCategoriesController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            var cats = _db.Categories.Select(c => new
            {
                c.Id,
                c.Name,
                c.Description,
                c.OrderIndex,
                c.IsActive,
                CourseCount = _db.Courses.Count(co => co.CategoryId == c.Id)
            }).ToList();

            return Ok(ResponseDto<object>.SuccessResult(cats));
        }

        [HttpPost]
        public IActionResult Create([FromBody] ManageCategoryRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 100)
                return BadRequest(ResponseDto<object>.FailResult("INVALID_NAME", "Tên danh mục không hợp lệ."));

            var exists = _db.Categories.Any(c => c.Name.ToLower() == request.Name.Trim().ToLower());
            if (exists)
                return Conflict(ResponseDto<object>.FailResult("DUPLICATE_CATEGORY", "Tên danh mục đã tồn tại."));

            var cat = new Category(request.Name.Trim(), request.Description, request.OrderIndex);
            if (request.IsActive) cat.Activate();

            _db.Categories.Add(cat);
            _db.SaveChanges();

            return Ok(ResponseDto<object>.SuccessResult(new { cat.Id }));
        }

        [HttpPut("{id:guid}")]
        public IActionResult Update([FromRoute] Guid id, [FromBody] ManageCategoryRequest request)
        {
            var cat = _db.Categories.FirstOrDefault(c => c.Id == id);
            if (cat == null) return NotFound(ResponseDto<object>.FailResult("NOT_FOUND", "Không tìm thấy danh mục."));

            if (_db.Categories.Any(c => c.Id != id && c.Name.ToLower() == request.Name.Trim().ToLower()))
                return Conflict(ResponseDto<object>.FailResult("DUPLICATE_CATEGORY", "Tên danh mục đã tồn tại."));

            cat.UpdateInfo(request.Name.Trim(), request.Description);
            cat.ChangeOrder(request.OrderIndex);
            if (request.IsActive) cat.Activate(); else cat.Deactivate();

            _db.SaveChanges();

            return Ok(ResponseDto<object>.SuccessResult(new { Message = "Category updated" }));
        }

        [HttpPatch("{id:guid}/status")]
        public IActionResult ChangeStatus([FromRoute] Guid id, [FromBody] ChangeCategoryStatusRequest request)
        {
            var cat = _db.Categories.FirstOrDefault(c => c.Id == id);
            if (cat == null) return NotFound(ResponseDto<object>.FailResult("NOT_FOUND", "Không tìm thấy danh mục."));

            var assigned = _db.Courses.Any(co => co.CategoryId == id);
            if (!request.IsActive && assigned)
                return BadRequest(ResponseDto<object>.FailResult("CATEGORY_HAS_COURSES", "Không thể vô hiệu hóa danh mục đang có khóa học."));

            if (request.IsActive) cat.Activate(); else cat.Deactivate();
            _db.SaveChanges();

            return Ok(ResponseDto<object>.SuccessResult(new
            {
                Message = "Category status updated",
                cat.Id,
                cat.IsActive
            }));
        }

        [HttpPut("reorder")]
        public IActionResult Reorder([FromBody] ReorderCategoriesRequest request)
        {
            if (request?.CategoryIds == null || request.CategoryIds.Count == 0)
                return BadRequest(ResponseDto<object>.FailResult("INVALID_ORDER", "Danh sách sắp xếp không hợp lệ."));

            if (request.CategoryIds.Any(id => id == Guid.Empty))
                return BadRequest(ResponseDto<object>.FailResult("INVALID_ORDER", "Danh sách sắp xếp không hợp lệ."));

            if (request.CategoryIds.Distinct().Count() != request.CategoryIds.Count)
                return BadRequest(ResponseDto<object>.FailResult("INVALID_ORDER", "Danh sách sắp xếp không hợp lệ."));

            var existingCategories = _db.Categories.Where(c => request.CategoryIds.Contains(c.Id)).ToList();
            if (existingCategories.Count != request.CategoryIds.Count)
                return BadRequest(ResponseDto<object>.FailResult("INVALID_ORDER", "Danh sách sắp xếp không hợp lệ."));

            for (var i = 0; i < request.CategoryIds.Count; i++)
            {
                var category = existingCategories.First(c => c.Id == request.CategoryIds[i]);
                category.ChangeOrder(i);
            }

            _db.SaveChanges();

            return Ok(ResponseDto<object>.SuccessResult(new { Message = "Category order updated" }));
        }

        [HttpDelete("{id:guid}")]
        public IActionResult Delete([FromRoute] Guid id)
        {
            var cat = _db.Categories.FirstOrDefault(c => c.Id == id);
            if (cat == null) return NotFound(ResponseDto<object>.FailResult("NOT_FOUND", "Không tìm thấy danh mục."));

            var assigned = _db.Courses.Any(co => co.CategoryId == id);
            if (assigned) return BadRequest(ResponseDto<object>.FailResult("CATEGORY_HAS_COURSES", "Không thể xóa danh mục đang có khóa học."));

            _db.Categories.Remove(cat);
            _db.SaveChanges();

            return Ok(ResponseDto<object>.SuccessResult(new { Message = "Category deleted" }));
        }
    }
}
