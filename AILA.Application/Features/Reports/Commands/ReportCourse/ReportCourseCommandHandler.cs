using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Reports.Dtos;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Reports.Commands.ReportCourse
{
    public class ReportCourseCommandHandler(IUnitOfWork uow)
        : IRequestHandler<ReportCourseCommand, ResponseDto<ReportCourseResponseDto>>
    {
        private const int MaxDescriptionLength = 1000;

        public async Task<ResponseDto<ReportCourseResponseDto>> Handle(
            ReportCourseCommand request, CancellationToken ct)
        {
            // AC-2 / Technical: reason bắt buộc & phải thuộc tập enum định nghĩa sẵn (server-side validation).
            // ReportType bắt đầu từ 1 nên giá trị 0/không chọn cũng bị loại tại đây.
            if (!Enum.IsDefined(typeof(ReportType), request.Reason))
                return ResponseDto<ReportCourseResponseDto>.FailResult(
                    "INVALID_REASON", "Lý do báo cáo không hợp lệ. Vui lòng chọn một lý do trong danh sách.");

            // Edge case: giới hạn độ dài mô tả để tránh lạm dụng/payload lớn.
            var description = request.Description?.Trim();
            if (description is { Length: > MaxDescriptionLength })
                return ResponseDto<ReportCourseResponseDto>.FailResult(
                    "VALIDATION_ERROR", $"Mô tả không được vượt quá {MaxDescriptionLength} ký tự.");

            // Khóa học phải tồn tại.
            var course = await uow.Courses.GetByIdAsync(request.CourseId);
            if (course == null)
                return ResponseDto<ReportCourseResponseDto>.FailResult(
                    "COURSE_NOT_FOUND", "Không tìm thấy khóa học.");

            // AC-5 / BR-02: chỉ Learner đã enroll mới được báo cáo (kiểm tra lại ngay tại thời điểm submit).
            var enrollment = await uow.Enrollments.GetByCourseAndLearnerAsync(request.CourseId, request.LearnerId, ct);
            if (enrollment == null)
                return ResponseDto<ReportCourseResponseDto>.FailResult(
                    "NOT_ENROLLED", "Bạn cần tham gia khóa học này trước khi báo cáo.");

            // Edge case: chống nộp trùng — đã có báo cáo đang chờ xử lý cho cùng khóa học.
            if (await uow.ContentReports.HasPendingCourseReportAsync(request.LearnerId, request.CourseId, ct))
                return ResponseDto<ReportCourseResponseDto>.FailResult(
                    "ALREADY_REPORTED", "Bạn đã báo cáo khóa học này và đang chờ xử lý.");

            // AC-3 / AC-4: tạo report ở trạng thái Pending (moderation queue) gắn với khóa học.
            // materialId = null vì đây là báo cáo cấp khóa học.
            var report = new ContentReport(request.LearnerId, request.CourseId, null, request.Reason, description);

            await uow.ContentReports.AddAsync(report);
            await uow.SaveChangesAsync(ct);

            var dto = new ReportCourseResponseDto(report.Id, report.Status.ToString(), report.CreatedAt);
            return ResponseDto<ReportCourseResponseDto>.SuccessResult(dto);
        }
    }
}
