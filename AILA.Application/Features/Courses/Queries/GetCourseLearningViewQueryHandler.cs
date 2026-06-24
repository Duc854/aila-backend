using AILA.Application.Common.Dtos;
using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.Courses.Queries
{
    public class GetCourseLearningViewQueryHandler :
        IRequestHandler<
            GetCourseLearningViewQuery,
            CourseLearningViewDto>
    {
        private readonly IUnitOfWork _uow;

        public GetCourseLearningViewQueryHandler(
            IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<CourseLearningViewDto> Handle(
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

            if (course == null)
                throw new Exception(
                    "Course not found");

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

                                Type =
                                    material.GetType().Name,

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

            return new CourseLearningViewDto
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
        }
    }
}
