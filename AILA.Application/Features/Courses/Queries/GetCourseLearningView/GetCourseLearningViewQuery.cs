using AILA.Application.Common.Dtos;
using AILA.Application.Common.Interfaces;
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
    public record GetCourseLearningViewQuery(Guid CourseId, Guid LearnerId) : IRequest<ResponseDto<CourseLearningViewDto>>;
}
