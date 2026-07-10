using AILA.Api.Extensions;
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

        [HttpPost("{tagId:guid}/verify")]
        public IActionResult VerifyTag([FromRoute] Guid tagId, [FromBody] VerifyTagRequest request)
        {
            var tag = _db.Tags.FirstOrDefault(t => t.Id == tagId && t.PublishRequest != null && !t.IsPublished);
            if (tag == null) return NotFound(ResponseDto<object>.FailResult("NOT_FOUND", "Không tìm thấy yêu cầu tag."));

            if (request.Decision == VerifyDecision.Approve)
            {
                // Approve: publish tag
                tag.Publish();

                // Also mark request approved (guard against null for analyzer)
                var publishRequest = tag.PublishRequest;
                if (publishRequest == null)
                {
                    _db.SaveChanges();
                    return Ok(ResponseDto<object>.SuccessResult(new { Message = "Tag approved" }));
                }

                publishRequest.Approve();
                // Audit
                var identity = HttpContext.GetUserIdentity();
                if (identity != null)
                {
                    _logger.LogInformation("Admin {AdminId} approved tag {TagId}", identity.UserId, tag.Id);
                }
                _db.SaveChanges();
                return Ok(ResponseDto<object>.SuccessResult(new { Message = "Tag approved" }));
            }

            // Reject
            if (string.IsNullOrWhiteSpace(request.RejectionReason))
                return BadRequest(ResponseDto<object>.FailResult("MISSING_REASON", "Lý do từ chối là bắt buộc."));

            // Remove tag from pending queue (delete)
            // Audit rejection
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

    public record CreateTagRequest(string Name);

    public enum VerifyDecision
    {
        Approve,
        Reject
    }

    public record VerifyTagRequest(VerifyDecision Decision, string? RejectionReason);
}
