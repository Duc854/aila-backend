using AILA.Application.Common.Dtos;
using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using MediatR;
using Shared.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.Courses.Queries.GetCourseLearningView
{
    public class GetCourseLearningViewQueryHandler :IRequestHandler<GetCourseLearningViewQuery,ResponseDto<CourseLearningViewDto>>
    {
        private readonly IUnitOfWork _uow;

        public GetCourseLearningViewQueryHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<ResponseDto<CourseLearningViewDto>> Handle(GetCourseLearningViewQuery request,CancellationToken cancellationToken)
        {
            var course = await _uow.Courses.GetCourseWithFullContentAsync(request.CourseId);
            if (course == null)
                return ResponseDto<CourseLearningViewDto>.FailResult("COURSE_NOT_FOUND", "Không tìm thấy thông tin khóa học.");
            var enrollment = await _uow.Enrollments.GetByCourseAndLearnerAsync(request.CourseId, request.LearnerId, cancellationToken);
            if (enrollment == null)
            {
                return ResponseDto<CourseLearningViewDto>.FailResult("ENROLLMENT_NOT_FOUND", "Không thể truy cập nội dung chi tiết khóa học do bạn chưa tham gia khóa học này");
            }
            if (!enrollment.Course.IsPublished)
            {
                return ResponseDto<CourseLearningViewDto>.FailResult("UNPUBLISH_COURSE", "Không thể truy cập nội dung chi tiết khóa học do khóa học đã bị ẩn");
            }
            var completedMaterialIds = await _uow.LearningProgresses.GetCompletedMaterialIdsAsync(request.CourseId, request.LearnerId);

            var completedIds = completedMaterialIds.ToHashSet();

            var currentMaterialId = await _uow.LearningProgresses.GetCurrentMaterialIdAsync(request.CourseId, request.LearnerId);
            //Cập nhật việc nếu như chưa từng học bài nào sẽ tự lấy bài đầu tiên
            if (currentMaterialId == null || currentMaterialId == Guid.Empty)
            {
                currentMaterialId = course.Modules
                    .OrderBy(m => m.OrderIndex)
                    .SelectMany(m => m.Materials
                        .OrderBy(material => material.OrderIndex))
                    .Select(material => (Guid?)material.Id)
                    .FirstOrDefault();
            }
            // CẬP NHẬT: Thực hiện mapping kèm OrderIndex và ModuleId, đồng thời OrderBy để cấu trúc cây chuẩn hóa
            var modules = course.Modules.OrderBy(m => m.OrderIndex).Select(m => new ModuleLearningDto
                {
                    Id = m.Id,
                    Title = m.Title,
                    OrderIndex = m.OrderIndex,
                    Materials = m.Materials.OrderBy(material => material.OrderIndex)
                        .Select(material => new MaterialLearningDto
                        {
                            Id = material.Id,
                            ModuleId = m.Id, // <--- CẬP NHẬT: Gán thêm ModuleId để FE tiện xử lý Next/Prev bài học
                            Title = material.Title,
                            OrderIndex = material.OrderIndex, // <--- CẬP NHẬT: Gán OrderIndex cho Material
                            Type = material.MaterialType.ToString(),
                            IsCompleted = completedIds.Contains(material.Id),
                        }).ToList()
                }).ToList();



            var totalMaterials = modules.Sum(x => x.Materials.Count);
            var learningViewDto = new CourseLearningViewDto
            {
                Progress = new CourseProgressDto
                {
                    EnrollmentId = enrollment.Id,
                    CompletedMaterials = completedIds.Count,
                    TotalMaterials = totalMaterials,
                    Percent = totalMaterials == 0
                        ? 0
                        : Math.Round(completedIds.Count * 100.0 / totalMaterials, 2),
                    CurrentMaterialId = currentMaterialId
                },
                Modules = modules
            };
            return ResponseDto<CourseLearningViewDto>.SuccessResult(learningViewDto);
        }
    }
}
