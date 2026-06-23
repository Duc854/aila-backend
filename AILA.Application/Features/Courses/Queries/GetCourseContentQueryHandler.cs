using AILA.Application.Common.Dtos;
using AILA.Application.Common.Interfaces;
using AILA.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.Courses.Queries
{
    public class GetCourseContentQueryHandler : IRequestHandler<GetCourseContentQuery, CourseContentDto?>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetCourseContentQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<CourseContentDto?> Handle(GetCourseContentQuery request, CancellationToken cancellationToken)
        {
            // 1. Kiểm tra thông tin ghi danh (Enrollment) thông qua Generic Repository
            var enrollments = await _unitOfWork.Repository<Enrollment>().GetAllAsync();
            var enrollment = enrollments.FirstOrDefault(e => e.CourseId == request.CourseId && e.LearnerId == request.LearnerId);

            if (enrollment == null) return null;

            // 2. Lấy danh sách ID các bài học đã hoàn thành thông qua Generic Repository
            var allProgress = await _unitOfWork.Repository<LearningProgress>().GetAllAsync();
            var completedMaterialIds = allProgress
                .Where(lp => lp.EnrollmentId == enrollment.Id)
                .Select(lp => lp.MaterialId)
                .ToHashSet();

            // 3. Gọi Specific Repository lấy toàn bộ cây dữ liệu khóa học đã nạp sẵn Eager Loading
            var course = await _unitOfWork.Courses.GetCourseWithFullContentAsync(request.CourseId);
            if (course == null) return null;

            // 4. Khớp và Mapping sang dữ liệu DTO sạch trả về Client
            return new CourseContentDto
            {
                CourseId = course.Id,
                Name = course.Name,
                Description = course.Description,
                ThumbnailUrl = course.ThumbnailUrl,
                ProgressPct = enrollment.ProgressPct,
                EnrollmentStatus = enrollment.Status.ToString(),
                Modules = (course.Modules ?? new List<Module>())
                    .Where(m => m.IsPublished)
                    .OrderBy(m => m.OrderIndex)
                    .Select(m => new ModuleContentDto
                    {
                        ModuleId = m.Id,
                        Title = m.Title,
                        OrderIndex = m.OrderIndex,
                        Materials = (m.Materials ?? new List<Material>())
                            .OrderBy(mat => mat.OrderIndex)
                            .Select(mat => new MaterialContentDto
                            {
                                MaterialId = mat.Id,
                                Title = mat.Title,
                                MaterialType = mat.MaterialType.ToString(),
                                OrderIndex = mat.OrderIndex,
                                IsCompleted = completedMaterialIds.Contains(mat.Id),
                                VideoDetails = mat.VideoDetails != null ? new VideoDetailDto
                                {
                                    VideoUrl = mat.VideoDetails.VideoUrl,
                                    ThumbnailUrl = mat.VideoDetails.ThumbnailUrl,
                                    DurationSeconds = mat.VideoDetails.DurationSeconds,
                                    Content = mat.VideoDetails.Content,
                                    CaptionsUrl = mat.VideoDetails.CaptionsUrl
                                } : null,
                                DocumentDetails = mat.DocumentDetails != null ? new DocumentDetailDto
                                {
                                    DocumentUrl = mat.DocumentDetails.DocumentUrl,
                                    Content = mat.DocumentDetails.Content,
                                    FileSizeKb = mat.DocumentDetails.FileSizeKb
                                } : null
                            }).ToList()
                    }).ToList()
            };
        }
    }
}
