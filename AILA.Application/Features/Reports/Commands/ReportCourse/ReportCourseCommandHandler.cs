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

            // Khóa học phải tồn tại (dù báo cáo cả khóa hay một học liệu, đều đi qua route khóa học này).
            var course = await uow.Courses.GetByIdAsync(request.CourseId);
            if (course == null)
                return ResponseDto<ReportCourseResponseDto>.FailResult(
                    "COURSE_NOT_FOUND", "Không tìm thấy khóa học.");

            // AC-5 / BR-02: chỉ Learner đã enroll mới được báo cáo (kiểm tra lại ngay tại thời điểm submit).
            var enrollment = await uow.Enrollments.GetByCourseAndLearnerAsync(request.CourseId, request.LearnerId, ct);
            if (enrollment == null)
                return ResponseDto<ReportCourseResponseDto>.FailResult(
                    "NOT_ENROLLED", "Bạn cần tham gia khóa học này trước khi báo cáo.");

            // Xác định đối tượng bị báo cáo: một học liệu cụ thể (nếu có MaterialId) hoặc cả khóa học.
            // ContentReport luôn gắn với courseId (required), materialId là tùy chọn.
            Guid? reportMaterialId = null;
            if (request.MaterialId is { } materialId)
            {
                // Học liệu phải tồn tại và thuộc đúng khóa học đang báo cáo.
                if (!await uow.Materials.IsMaterialInCourseAsync(materialId, request.CourseId, ct))
                    return ResponseDto<ReportCourseResponseDto>.FailResult(
                        "MATERIAL_NOT_FOUND", "Không tìm thấy học liệu trong khóa học này.");

                reportMaterialId = materialId;
            }

            // Edge case: chống nộp trùng — đã có báo cáo đang chờ xử lý cho cùng đối tượng.
            if (await uow.ContentReports.HasPendingReportAsync(request.LearnerId, request.CourseId, reportMaterialId, ct))
                return ResponseDto<ReportCourseResponseDto>.FailResult(
                    "ALREADY_REPORTED",
                    reportMaterialId != null
                        ? "Bạn đã báo cáo học liệu này và đang chờ xử lý."
                        : "Bạn đã báo cáo khóa học này và đang chờ xử lý.");

            // AC-3 / AC-4: tạo report ở trạng thái Pending (moderation queue).
            var report = new ContentReport(request.LearnerId, request.CourseId, reportMaterialId, request.Reason, description);

            await uow.ContentReports.AddAsync(report);
            await uow.SaveChangesAsync(ct);

            var dto = new ReportCourseResponseDto(report.Id, report.Status.ToString(), report.CreatedAt);
            return ResponseDto<ReportCourseResponseDto>.SuccessResult(dto);
        }
    }
}
