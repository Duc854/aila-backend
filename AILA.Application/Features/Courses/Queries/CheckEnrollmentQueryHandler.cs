using AILA.Application.Common.Interfaces;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Courses.Queries
{
    public class CheckEnrollmentQueryHandler
        : IRequestHandler<CheckEnrollmentQuery, ResponseDto<CheckEnrollmentResult>>
    {
        private readonly IUnitOfWork _uow;

        public CheckEnrollmentQueryHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<ResponseDto<CheckEnrollmentResult>> Handle(
            CheckEnrollmentQuery request,
            CancellationToken cancellationToken)
        {
            var enrollment = await _uow.Enrollments
                .GetByLearnerAndCourseAsync(request.LearnerId, request.CourseId);

            var result = new CheckEnrollmentResult
            {
                IsEnrolled = enrollment != null,
                EnrolledAt = enrollment?.EnrolledAt,
                Status     = enrollment?.Status.ToString()
            };

            return ResponseDto<CheckEnrollmentResult>.SuccessResult(result);
        }
    }
}
