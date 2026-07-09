using AILA.Api.Extensions;
using AILA.Application.Features.Quizzes.Commands.StartQuizAttempt;
using AILA.Application.Features.Quizzes.Commands.SubmitQuiz;
using AILA.Application.Features.Quizzes.Dtos;
using AILA.Application.Features.Quizzes.Queries.GetQuizResultDetail;
using AILA.Application.Features.Quizzes.Queries.GetQuizResultSummary;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared.Wrappers;

namespace AILA.Api.Controllers
{
    [ApiController]
    [Route("api/courses/{courseId:guid}/materials/{materialId:guid}/quiz")]
    public class QuizzesController : ControllerBase
    {
        private readonly ISender _sender;

        public QuizzesController(ISender sender)
        {
            _sender = sender;
        }

        /// <summary>
        /// Bắt đầu (hoặc tiếp tục) một lượt làm bài kiểm tra. Trả về câu hỏi + cấu hình,
        /// không kèm đáp án đúng. Yêu cầu Learner đã đăng ký khóa học (UC-26, AC-1).
        /// </summary>
        [HttpPost("start")]
        [Authorize(Roles = "Learner")]
        public async Task<IActionResult> Start(
            [FromRoute] Guid courseId, [FromRoute] Guid materialId)
        {
            var identity = HttpContext.GetUserIdentity();
            if (identity == null)
            {
                return Unauthorized(ResponseDto<object>.FailResult(
                    "UNAUTHORIZED", "Xác thực người dùng thất bại hoặc mã token không hợp lệ."));
            }

            var command = new StartQuizAttemptCommand(courseId, materialId, identity.UserId);
            var result = await _sender.Send(command, HttpContext.RequestAborted);

            return result.Success ? Ok(result) : MapError(result.ErrorCode, result);
        }

        /// <summary>
        /// Nộp bài kiểm tra. System chấm điểm phía server, lưu kết quả (bất biến) và
        /// cập nhật tiến độ trong cùng một giao dịch (UC-26, AC-2..AC-8).
        /// </summary>
        [HttpPost("{attemptId:guid}/submit")]
        [Authorize(Roles = "Learner")]
        public async Task<IActionResult> Submit(
            [FromRoute] Guid courseId,
            [FromRoute] Guid materialId,
            [FromRoute] Guid attemptId,
            [FromBody] SubmitQuizRequest request)
        {
            var identity = HttpContext.GetUserIdentity();
            if (identity == null)
            {
                return Unauthorized(ResponseDto<object>.FailResult(
                    "UNAUTHORIZED", "Xác thực người dùng thất bại hoặc mã token không hợp lệ."));
            }

            var command = new SubmitQuizCommand(
                courseId,
                materialId,
                attemptId,
                identity.UserId,
                request?.Answers ?? new List<QuizAnswerSubmissionDto>());

            var result = await _sender.Send(command, HttpContext.RequestAborted);

            return result.Success ? Ok(result) : MapError(result.ErrorCode, result);
        }

        /// <summary>
        /// Xem tóm tắt kết quả bài kiểm tra — lần làm mới nhất (UC-27, AC-1/AC-2/AC-5). Read-only.
        /// Trả về empty state (hasResult=false) nếu Learner chưa từng nộp bài.
        /// </summary>
        [HttpGet("result")]
        [Authorize(Roles = "Learner")]
        public async Task<IActionResult> GetResult(
            [FromRoute] Guid courseId, [FromRoute] Guid materialId)
        {
            var identity = HttpContext.GetUserIdentity();
            if (identity == null)
            {
                return Unauthorized(ResponseDto<object>.FailResult(
                    "UNAUTHORIZED", "Xác thực người dùng thất bại hoặc mã token không hợp lệ."));
            }

            var query = new GetQuizResultSummaryQuery(courseId, materialId, identity.UserId);
            var result = await _sender.Send(query, HttpContext.RequestAborted);

            return result.Success ? Ok(result) : MapError(result.ErrorCode, result);
        }

        /// <summary>
        /// Xem chi tiết kết quả (review từng câu + đáp án đúng) của lần làm mới nhất (UC-27, AC-3).
        /// Bị chặn nếu quiz cấu hình ẩn đáp án (AC-4 → 403 ANSWERS_HIDDEN). Read-only.
        /// </summary>
        [HttpGet("result/detail")]
        [Authorize(Roles = "Learner")]
        public async Task<IActionResult> GetResultDetail(
            [FromRoute] Guid courseId, [FromRoute] Guid materialId)
        {
            var identity = HttpContext.GetUserIdentity();
            if (identity == null)
            {
                return Unauthorized(ResponseDto<object>.FailResult(
                    "UNAUTHORIZED", "Xác thực người dùng thất bại hoặc mã token không hợp lệ."));
            }

            var query = new GetQuizResultDetailQuery(courseId, materialId, identity.UserId);
            var result = await _sender.Send(query, HttpContext.RequestAborted);

            return result.Success ? Ok(result) : MapError(result.ErrorCode, result);
        }

        // Ánh xạ ErrorCode nghiệp vụ sang mã HTTP tương ứng.
        private IActionResult MapError<T>(string? errorCode, ResponseDto<T> result) => errorCode switch
        {
            "ENROLLMENT_NOT_FOUND" => StatusCode(StatusCodes.Status403Forbidden, result),
            "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, result),
            "ANSWERS_HIDDEN" => StatusCode(StatusCodes.Status403Forbidden, result),
            "QUIZ_NOT_FOUND" => NotFound(result),
            "ATTEMPT_NOT_FOUND" => NotFound(result),
            "NO_RESULT" => NotFound(result),
            "QUIZ_NOT_CONFIGURED" => UnprocessableEntity(result),
            "INVALID_SUBMISSION" => BadRequest(result),
            _ => BadRequest(result)
        };
    }
}
