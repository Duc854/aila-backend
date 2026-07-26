using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Reports.Dtos;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Reports.Commands.ReportCourse
{
    public class ReportCourseCommandHandler : IRequestHandler<ReportCourseCommand, ResponseDto<ReportCourseResponseDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private const int MaxDescriptionLength = 1000;

        public ReportCourseCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ResponseDto<ReportCourseResponseDto>> Handle(
            ReportCourseCommand request,
            CancellationToken cancellationToken)
        {
            // Validate Reason
            if (!Enum.IsDefined(typeof(ReportType), request.Reason))
            {
                return ResponseDto<ReportCourseResponseDto>.FailResult(
                    "INVALID_REASON",
                    "Lý do báo cáo không hợp lệ.");
            }

            // Validate Description
            var description = request.Description?.Trim();
            if (description?.Length > MaxDescriptionLength)
            {
                return ResponseDto<ReportCourseResponseDto>.FailResult(
                    "DESCRIPTION_TOO_LONG",
                    $"Mô tả không được vượt quá {MaxDescriptionLength} ký tự.");
            }

            // Khóa học phải tồn tại (dù báo cáo cả khóa hay một học liệu, đều đi qua route khóa học này).
            var course = await _unitOfWork.Courses.GetByIdAsync(request.CourseId);            // Check course exists
            if (course == null)
            {
                return ResponseDto<ReportCourseResponseDto>.FailResult(
                    "COURSE_NOT_FOUND",
                    "Không tìm thấy khóa học.");
            }

            // Check if learner is enrolled
            var enrollment = await _unitOfWork.Enrollments.GetByCourseAndLearnerAsync(
                request.CourseId,
                request.LearnerId,
                cancellationToken);

            if (enrollment == null)
            {
                return ResponseDto<ReportCourseResponseDto>.FailResult(
                    "NOT_ENROLLED",
                    "Bạn cần tham gia khóa học này trước khi báo cáo.");
            }

            // Xác định đối tượng bị báo cáo: một học liệu cụ thể (nếu có MaterialId) hoặc cả khóa học.
            // ContentReport chỉ gắn với đúng một đối tượng (XOR) nên chỉ một trong hai ID được set.
            Guid? reportCourseId;
            Guid? reportMaterialId;
            if (request.MaterialId is { } materialId)
            {
                // Học liệu phải tồn tại và thuộc đúng khóa học đang báo cáo.
                if (!await _unitOfWork.Materials.IsMaterialInCourseAsync(materialId, request.CourseId, cancellationToken))
                    return ResponseDto<ReportCourseResponseDto>.FailResult(
                        "MATERIAL_NOT_FOUND", "Không tìm thấy học liệu trong khóa học này.");

                reportCourseId = null;
                reportMaterialId = materialId;
            }
            else
            {
                reportCourseId = request.CourseId;
                reportMaterialId = null;
            }

            // Edge case: chống nộp trùng — đã có báo cáo đang chờ xử lý cho cùng đối tượng.
            if (await _unitOfWork.ContentReports.HasPendingReportAsync(request.LearnerId, reportCourseId, reportMaterialId, cancellationToken))
            {
                return ResponseDto<ReportCourseResponseDto>.FailResult(
                    "ALREADY_REPORTED",
                    reportMaterialId != null
                        ? "Bạn đã báo cáo học liệu này và đang chờ xử lý."
                        : "Bạn đã báo cáo khóa học này và đang chờ xử lý.");
            }

            ContentReport report;
            if (request.MaterialId.HasValue)
            {
                report = ContentReport.CreateMaterialReport(
                    request.LearnerId,
                    request.CourseId,
                    request.MaterialId.Value,
                    request.Reason,
                    description);
            }
            else
            {
                report = ContentReport.CreateCourseReport(
                    request.LearnerId,
                    request.CourseId,
                    request.Reason,
                    description);
            }

            await _unitOfWork.ContentReports.AddAsync(report);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ResponseDto<ReportCourseResponseDto>.SuccessResult(
                new ReportCourseResponseDto(report.Id, report.Status.ToString(), report.CreatedAt));
        }
    }
}