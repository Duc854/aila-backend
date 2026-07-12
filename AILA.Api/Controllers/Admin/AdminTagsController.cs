using AILA.Api.Extensions;
using AILA.Application.Features.Tags.Dtos;
using AILA.Infrastructure.Persistence;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Wrappers;

namespace AILA.Api.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/tags")]
    [Authorize(Roles = "Admin")]
    public class AdminTagsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<AdminTagsController> _logger;

        public AdminTagsController(ApplicationDbContext db, ILogger<AdminTagsController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult GetPublished()
        {
            var tagsRaw = _db.Tags.Where(t => t.IsPublished).ToList();

            var tags = tagsRaw.Select(t => new
            {
                t.Id,
                t.Name,
                t.Code,
                t.IsPublished,
                t.CreatedById,
                Source = t.CreatedById == null ? "Admin" : "Expert",
                UsageCount = _db.Courses.Count(c => c.CourseTags.Any(ct => ct.Id == t.Id))
            }).ToList();

            return Ok(ResponseDto<object>.SuccessResult(tags));
        }

        [HttpPost]
        public IActionResult CreateTag([FromBody] CreateTagRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 100)
                return BadRequest(ResponseDto<object>.FailResult("INVALID_NAME", "Tên tag không hợp lệ."));

            var code = request.Name.Trim().ToLower().Replace(" ", "-");
            if (_db.Tags.Any(t => t.Code == code || t.Name.ToLower() == request.Name.Trim().ToLower()))
                return Conflict(ResponseDto<object>.FailResult("DUPLICATE_TAG", "Tag đã tồn tại."));

            var tag = Tag.CreateByAdmin(request.Name.Trim(), code);
            _db.Tags.Add(tag);
            _db.SaveChanges();

            return Ok(ResponseDto<object>.SuccessResult(new { tag.Id, tag.Code }));
        }

        [HttpGet("pending")]
        public IActionResult GetPending()
        {
            var pendingTags = _db.Tags.Where(t => !t.IsPublished && t.PublishRequest != null).ToList();

            var createdByIds = pendingTags.Where(t => t.CreatedById != null).Select(t => t.CreatedById!.Value).Distinct().ToList();
            var users = _db.Users.Where(u => createdByIds.Contains(u.Id)).ToDictionary(u => u.Id, u => u.FullName);

            var pending = pendingTags.Select(t => new
            {
                t.Id,
                t.Name,
                t.Code,
                t.CreatedById,
                SubmittedBy = t.CreatedById != null && users.ContainsKey(t.CreatedById.Value) ? users[t.CreatedById.Value] : null,
                RequestStatus = t.PublishRequest != null ? (TagPublishRequestStatus?)t.PublishRequest.Status : null,
                SubmittedAt = t.PublishRequest != null ? t.PublishRequest.CreatedAt : (DateTime?)null
            }).ToList();

            return Ok(ResponseDto<object>.SuccessResult(pending));
        }

        [HttpPut("{tagId:guid}")]
        public IActionResult UpdateTag([FromRoute] Guid tagId, [FromBody] UpdateTagRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 100)
                return BadRequest(ResponseDto<object>.FailResult("INVALID_NAME", "Tên tag không hợp lệ."));

            var tag = _db.Tags.FirstOrDefault(t => t.Id == tagId);
            if (tag == null)
                return NotFound(ResponseDto<object>.FailResult("NOT_FOUND", "Không tìm thấy tag."));

            if (tag.CreatedById != null && tag.IsPublished)
                return BadRequest(ResponseDto<object>.FailResult("CUSTOM_TAG_NOT_UPDATABLE", "Tag do chuyên gia tạo đã được công khai nên không thể cập nhật."));

            var assignedToCourse = _db.Courses.Any(c => c.CourseTags.Any(ct => ct.Id == tagId));
            if (assignedToCourse)
                return BadRequest(ResponseDto<object>.FailResult("TAG_IN_USE", "Tag đang được sử dụng không thể cập nhật."));

            var normalizedName = request.Name.Trim();
            var normalizedCode = normalizedName.ToLower().Replace(" ", "-");
            if (_db.Tags.Any(t => t.Id != tagId && (t.Code == normalizedCode || t.Name.ToLower() == normalizedName.ToLower())))
                return Conflict(ResponseDto<object>.FailResult("DUPLICATE_TAG", "Tag đã tồn tại."));

            var tagEntry = _db.Entry(tag);
            tagEntry.Property(t => t.Name).CurrentValue = normalizedName;
            tagEntry.Property(t => t.Code).CurrentValue = normalizedCode;
            tag.UpdateTimestamp();
            _db.SaveChanges();

            return Ok(ResponseDto<object>.SuccessResult(new { Message = "Tag updated" }));
        }

        [HttpDelete("{tagId:guid}")]
        public IActionResult DeleteTag([FromRoute] Guid tagId)
        {
            var tag = _db.Tags.FirstOrDefault(t => t.Id == tagId);
            if (tag == null)
                return NotFound(ResponseDto<object>.FailResult("NOT_FOUND", "Không tìm thấy tag."));

            if (tag.CreatedById != null && tag.IsPublished)
                return BadRequest(ResponseDto<object>.FailResult("CUSTOM_TAG_NOT_REMOVABLE", "Tag do chuyên gia tạo không thể xóa."));

            var assignedToCourse = _db.Courses.Any(c => c.CourseTags.Any(ct => ct.Id == tagId));
            if (assignedToCourse)
                return BadRequest(ResponseDto<object>.FailResult("TAG_IN_USE", "Tag đang được sử dụng không thể xóa."));

            _db.Tags.Remove(tag);
            _db.SaveChanges();

            return Ok(ResponseDto<object>.SuccessResult(new { Message = "Tag removed" }));
        }

        [HttpPost("{tagId:guid}/verify")]
        public IActionResult VerifyTag([FromRoute] Guid tagId, [FromBody] VerifyTagRequest request)
        {
            var tag = _db.Tags.FirstOrDefault(t => t.Id == tagId && t.PublishRequest != null && !t.IsPublished);
            if (tag == null) return NotFound(ResponseDto<object>.FailResult("NOT_FOUND", "Không tìm thấy yêu cầu tag."));

            if (request.Decision == VerifyDecision.Approve)
            {
                tag.Publish();
                var publishRequest = tag.PublishRequest;
                if (publishRequest == null)
                {
                    _db.SaveChanges();
                    return Ok(ResponseDto<object>.SuccessResult(new { Message = "Tag approved" }));
                }

                publishRequest.Approve();
                var identity = HttpContext.GetUserIdentity();
                if (identity != null)
                {
                    _logger.LogInformation("Admin {AdminId} approved tag {TagId}", identity.UserId, tag.Id);
                }
                _db.SaveChanges();
                return Ok(ResponseDto<object>.SuccessResult(new { Message = "Tag approved" }));
            }

            if (string.IsNullOrWhiteSpace(request.RejectionReason))
                return BadRequest(ResponseDto<object>.FailResult("MISSING_REASON", "Lý do từ chối là bắt buộc."));

            var iden = HttpContext.GetUserIdentity();
            if (iden != null)
            {
                _logger.LogInformation("Admin {AdminId} rejected tag {TagId}. Reason: {Reason}", iden.UserId, tag.Id, request.RejectionReason);
            }
            _db.Tags.Remove(tag);
            _db.SaveChanges();

            return Ok(ResponseDto<object>.SuccessResult(new { Message = "Tag rejected and removed" }));
        }
    }
}
