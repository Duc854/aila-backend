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
    public record GetCourseLearningViewQuery(Guid CourseId, Guid LearnerId) : IRequest<CourseLearningViewDto?>;
}
