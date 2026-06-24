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
    public class GetCourseLearningViewQueryHandler :
            IRequestHandler<
                GetCourseLearningViewQuery,
                ResponseDto<CourseLearningViewDto>> // CẬP NHẬT: Thêm Wrapper vào kiểu trả về của Request
    {
        private readonly IUnitOfWork _uow;

        public GetCourseLearningViewQueryHandler(
            IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<ResponseDto<CourseLearningViewDto>> Handle(
            GetCourseLearningViewQuery request,
            CancellationToken cancellationToken)
        {
            var courseTask =
                _uow.Courses
                .GetCourseWithFullContentAsync(
                    request.CourseId);

            var completedTask =
                _uow.LearningProgresses
                .GetCompletedMaterialIdsAsync(
                    request.CourseId,
                    request.LearnerId);

            var currentTask =
                _uow.LearningProgresses
                .GetCurrentMaterialIdAsync(
                    request.CourseId,
                    request.LearnerId);

            await Task.WhenAll(
                courseTask,
                completedTask,
                currentTask);

            var course = await courseTask;

            // CẬP NHẬT: Thay vì throw Exception bừa bãi làm sập API, trả về FailResult chuẩn hóa
            if (course == null)
                return ResponseDto<CourseLearningViewDto>.FailResult("COURSE_NOT_FOUND", "Không tìm thấy thông tin khóa học.");

            var completedIds =
                (await completedTask)
                .ToHashSet();

            var currentMaterialId =
                await currentTask;

            var modules =
                course.Modules
                .Select(m => new ModuleLearningDto
                {
                    Id = m.Id,

                    Title = m.Title,

                    Materials =
                        m.Materials
                        .Select(material =>
                            new MaterialLearningDto
                            {
                                Id = material.Id,

                                Title =
                                    material.Title,

                                // CẬP NHẬT: Sử dụng thuộc tính enum MaterialType thay vì GetType().Name 
                                // để tránh việc luôn hiển thị chữ "Material" do cơ chế Proxy/Entity cơ sở của EF Core.
                                Type =
                                    material.MaterialType.ToString(),

                                IsCompleted =
                                    completedIds.Contains(
                                        material.Id),

                                IsCurrent =
                                    currentMaterialId
                                    == material.Id
                            })
                        .ToList()
                })
                .ToList();

            var totalMaterials =
                modules.Sum(
                    x => x.Materials.Count);

            // Khởi tạo Dto kết quả
            var learningViewDto = new CourseLearningViewDto
            {
                Progress =
                    new CourseProgressDto
                    {
                        CompletedMaterials =
                            completedIds.Count,

                        TotalMaterials =
                            totalMaterials,

                        Percent =
                            totalMaterials == 0
                            ? 0
                            : completedIds.Count
                                * 100.0
                                / totalMaterials,

                        CurrentMaterialId =
                            currentMaterialId
                    },

                Modules = modules
            };

            // CẬP NHẬT: Bọc kết quả vào hàm SuccessResult
            return ResponseDto<CourseLearningViewDto>.SuccessResult(learningViewDto);
        }
    }
}
