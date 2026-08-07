using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Experts.Dtos;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using MediatR;
using Shared.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Features.Experts.Queries.GetExpertDashboard
{
    public record GetExpertDashboardQuery(
        Guid ExpertId,
        Guid? CourseId = null,
        string ReportingPeriod = "Last30Days",
        DateTime? StartDate = null,
        DateTime? EndDate = null
    ) : IRequest<ResponseDto<ExpertDashboardDto>>;

    public class GetExpertDashboardQueryHandler 
        : IRequestHandler<GetExpertDashboardQuery, ResponseDto<ExpertDashboardDto>>
    {
        private readonly IUnitOfWork _uow;

        public GetExpertDashboardQueryHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<ResponseDto<ExpertDashboardDto>> Handle(
            GetExpertDashboardQuery request, 
            CancellationToken cancellationToken)
        {
            // 1. Kiểm tra danh sách khóa học đã xuất bản của Expert - BR-01, AF-01
            var publishedCourses = await _uow.ExpertDashboards.GetPublishedCoursesByExpertAsync(
                request.ExpertId, cancellationToken);

            var availableCourseOptions = publishedCourses.Select(c => new CourseOptionDto
            {
                Id = c.Id,
                Name = c.Name
            }).ToList();

            // AF-01: Nếu Expert chưa có khóa học xuất bản nào
            if (publishedCourses.Count == 0)
            {
                return ResponseDto<ExpertDashboardDto>.SuccessResult(new ExpertDashboardDto
                {
                    HasPublishedCourses = false,
                    HasData = false,
                    Message = "Bạn chưa có khóa học nào được xuất bản.",
                    Overview = new DashboardOverviewStatsDto(),
                    Trends = new List<TrendPointDto>(),
                    CoursePerformances = new List<CoursePerformanceSummaryDto>(),
                    AvailableCourses = new List<CourseOptionDto>()
                });
            }

            // 2. Validate phạm vi báo cáo (Reporting Scope) - AF-03, BR-03
            var period = string.IsNullOrWhiteSpace(request.ReportingPeriod) 
                ? "Last30Days" 
                : request.ReportingPeriod.Trim();

            DateTime fromDate;
            DateTime toDate = DateTime.UtcNow;

            switch (period.ToLower())
            {
                case "last7days":
                    fromDate = toDate.Date.AddDays(-6);
                    break;
                case "last30days":
                    fromDate = toDate.Date.AddDays(-29);
                    break;
                case "last90days":
                    fromDate = toDate.Date.AddDays(-89);
                    break;
                case "thisyear":
                    fromDate = new DateTime(toDate.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    break;
                case "customrange":
                    if (!request.StartDate.HasValue || !request.EndDate.HasValue)
                    {
                        return ResponseDto<ExpertDashboardDto>.FailResult(
                            "INVALID_REPORTING_SCOPE", 
                            "Khoảng thời gian tùy chỉnh phải bao gồm ngày bắt đầu và ngày kết thúc.");
                    }
                    if (request.StartDate.Value > request.EndDate.Value)
                    {
                        return ResponseDto<ExpertDashboardDto>.FailResult(
                            "INVALID_REPORTING_SCOPE", 
                            "Ngày bắt đầu không được sau ngày kết thúc.");
                    }
                    fromDate = request.StartDate.Value.Date;
                    toDate = request.EndDate.Value.Date.AddDays(1).AddTicks(-1);
                    break;
                default:
                    return ResponseDto<ExpertDashboardDto>.FailResult(
                        "INVALID_REPORTING_SCOPE", 
                        "Thời gian báo cáo không hợp lệ (Chỉ hỗ trợ Last7Days, Last30Days, Last90Days, ThisYear, CustomRange).");
            }

            // Kiểm tra CourseId nếu người dùng lọc theo 1 khóa học cụ thể
            List<Guid> targetCourseIds;
            if (request.CourseId.HasValue)
            {
                var targetCourse = publishedCourses.FirstOrDefault(c => c.Id == request.CourseId.Value);
                if (targetCourse == null)
                {
                    return ResponseDto<ExpertDashboardDto>.FailResult(
                        "INVALID_REPORTING_SCOPE", 
                        "Khóa học được chọn không hợp lệ hoặc không thuộc danh sách khóa học đã xuất bản của bạn.");
                }
                targetCourseIds = new List<Guid> { request.CourseId.Value };
            }
            else
            {
                targetCourseIds = publishedCourses.Select(c => c.Id).ToList();
            }

            // 3. Lấy dữ liệu phân tích trong phạm vi (BR-04, BR-05)
            var enrollments = await _uow.ExpertDashboards.GetEnrollmentsInScopeAsync(
                targetCourseIds, fromDate, toDate, cancellationToken);

            var totalQuizAttempts = await _uow.ExpertDashboards.GetQuizAttemptsCountInScopeAsync(
                targetCourseIds, fromDate, toDate, cancellationToken);

            var totalPracticeAttempts = await _uow.ExpertDashboards.GetPracticeAttemptsCountInScopeAsync(
                targetCourseIds, fromDate, toDate, cancellationToken);

            // AF-02: Không có dữ liệu phân tích trong khoảng thời gian đã chọn
            bool hasData = enrollments.Count > 0 || totalQuizAttempts > 0 || totalPracticeAttempts > 0;
            string? dataMessage = hasData ? null : "Không có dữ liệu thống kê trong khoảng thời gian đã chọn.";

            // 4. Tính toán các chỉ số tổng quan (Overview Stats)
            int totalEnrollments = enrollments.Count;
            int totalActiveLearners = enrollments.Select(e => e.LearnerId).Distinct().Count();
            int completedEnrollmentsCount = enrollments.Count(e => e.Status == EnrollmentStatus.Completed);
            decimal averageCompletionRate = enrollments.Count > 0 
                ? Math.Round(enrollments.Average(e => e.ProgressPct), 2) 
                : 0.00m;

            var overview = new DashboardOverviewStatsDto
            {
                TotalPublishedCourses = publishedCourses.Count,
                TotalEnrollments = totalEnrollments,
                TotalActiveLearners = totalActiveLearners,
                AverageCompletionRate = averageCompletionRate,
                CompletedEnrollmentsCount = completedEnrollmentsCount,
                TotalQuizAttempts = totalQuizAttempts,
                TotalPracticeAttempts = totalPracticeAttempts
            };

            // 5. Tính toán dữ liệu biểu đồ xu hướng (Trends Chart)
            var trendPoints = new List<TrendPointDto>();
            int dayStep = (toDate - fromDate).Days <= 31 ? 1 : Math.Max(1, (toDate - fromDate).Days / 15);

            for (var curr = fromDate.Date; curr <= toDate.Date; curr = curr.AddDays(dayStep))
            {
                var nextCurr = curr.AddDays(dayStep);
                var periodEnrollments = enrollments
                    .Where(e => e.EnrolledAt >= curr && e.EnrolledAt < nextCurr)
                    .ToList();

                var periodActiveLearners = enrollments
                    .Where(e => (e.LastAccessedAt.HasValue && e.LastAccessedAt.Value >= curr && e.LastAccessedAt.Value < nextCurr)
                             || (e.EnrolledAt >= curr && e.EnrolledAt < nextCurr))
                    .Select(e => e.LearnerId)
                    .Distinct()
                    .Count();

                trendPoints.Add(new TrendPointDto
                {
                    DateLabel = curr.ToString("dd/MM"),
                    Enrollments = periodEnrollments.Count,
                    ActiveLearners = periodActiveLearners
                });
            }

            // 6. Tính toán hiệu suất theo từng khóa học (Course Performances)
            var targetCourses = publishedCourses.Where(c => targetCourseIds.Contains(c.Id)).ToList();
            var coursePerformances = targetCourses.Select(course =>
            {
                var courseEnrollments = enrollments.Where(e => e.CourseId == course.Id).ToList();
                decimal completionRate = courseEnrollments.Count > 0 
                    ? Math.Round(courseEnrollments.Average(e => e.ProgressPct), 2) 
                    : 0.00m;

                return new CoursePerformanceSummaryDto
                {
                    CourseId = course.Id,
                    CourseName = course.Name,
                    TotalEnrollments = courseEnrollments.Count,
                    CompletionRate = completionRate
                };
            }).OrderByDescending(c => c.TotalEnrollments).ToList();

            // 7. Tạo DTO trả về kết quả thành công
            var dto = new ExpertDashboardDto
            {
                HasPublishedCourses = true,
                HasData = hasData,
                Message = dataMessage,
                Overview = overview,
                Trends = trendPoints,
                CoursePerformances = coursePerformances,
                AvailableCourses = availableCourseOptions
            };

            return ResponseDto<ExpertDashboardDto>.SuccessResult(dto);
        }
    }
}
