using AILA.Application.Features.Categories.Commands.ChangeCategoryStatus;
using AILA.Application.Features.Categories.Commands.CreateCategory;
using AILA.Application.Features.Categories.Commands.ReorderCategories;
using AILA.Application.Features.Categories.Commands.UpdateCategory;
using AILA.Application.Features.Categories.Dtos;
using AILA.Application.Features.Categories.Queries.GetCategories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AILA.Api.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/categories")]
    [Authorize(Roles = "Admin")]
    public class AdminCategoriesController : ControllerBase
    {
        private readonly ISender _sender;

        public AdminCategoriesController(ISender sender)
        {
            _sender = sender;
        }

        /// <summary>
        /// UC-80 - Get Course Categories
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCategories(CancellationToken ct)
        {
            var result = await _sender.Send(new GetCategoriesQuery(), ct);
            return Ok(result);
        }

        /// <summary>
        /// UC-81 - Create Course Category
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateCategoryRequest request,
            CancellationToken ct)
        {
            var result = await _sender.Send(
                new CreateCategoryCommand(
                    request.Name,
                    request.Description,
                    request.OrderIndex),
                ct);

            return Ok(result);
        }

        /// <summary>
        /// UC-82 - Update Course Category
        /// </summary>
        [HttpPut("{categoryId:guid}")]
        public async Task<IActionResult> Update(
            Guid categoryId,
            [FromBody] UpdateCategoryRequest request,
            CancellationToken ct)
        {
            var result = await _sender.Send(
                new UpdateCategoryCommand(
                    categoryId,
                    request.Name,
                    request.Description),
                ct);

            return Ok(result);
        }

        /// <summary>
        /// UC-83 - Change Course Category Status
        /// </summary>
        [HttpPatch("{categoryId:guid}/status")]
        public async Task<IActionResult> ChangeStatus(
            Guid categoryId,
            [FromBody] ChangeCategoryStatusRequest request,
            CancellationToken ct)
        {
            var result = await _sender.Send(
                new ChangeCategoryStatusCommand(
                    categoryId,
                    request.IsActive),
                ct);

            return Ok(result);
        }

        /// <summary>
        /// UC-84 - Reorder Course Categories
        /// </summary>
        [HttpPut("reorder")]
        public async Task<IActionResult> Reorder(
            [FromBody] ReorderCategoriesRequest request,
            CancellationToken ct)
        {
            var result = await _sender.Send(
                new ReorderCategoriesCommand(request.CategoryIds),
                ct);

            return Ok(result);
        }
    }
}