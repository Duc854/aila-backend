using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Courses.Queries
{
    public record CheckEnrollmentQuery(Guid CourseId, Guid LearnerId)
        : IRequest<ResponseDto<CheckEnrollmentResult>>;

    public class CheckEnrollmentResult
    {
        public bool IsEnrolled { get; init; }
        public DateTime? EnrolledAt { get; init; }
        public string? Status { get; init; }
    }
}
