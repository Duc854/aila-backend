using AILA.Application.Common.Dtos.Rag;
using AILA.Application.Common.Exceptions;
using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Features.Rag.Commands.CreateCourseChatSession;

public class CreateCourseChatSessionCommandHandler
    : IRequestHandler<CreateCourseChatSessionCommand, CourseChatSessionDto>
{
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly ICourseChatSessionRepository _courseChatSessionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCourseChatSessionCommandHandler(
        IEnrollmentRepository enrollmentRepository,
        ICourseChatSessionRepository courseChatSessionRepository,
        IUnitOfWork unitOfWork)
    {
        _enrollmentRepository = enrollmentRepository;
        _courseChatSessionRepository = courseChatSessionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CourseChatSessionDto> Handle(
        CreateCourseChatSessionCommand request,
        CancellationToken cancellationToken)
    {
        var isEnrolled =
            await _enrollmentRepository.IsLearnerEnrolledInCourseAsync(
                request.AccountId,
                request.CourseId,
                cancellationToken);

        if (!isEnrolled)
        {
            throw new BusinessRuleException(
                "Bạn cần đăng ký khóa học này trước khi sử dụng Trợ lý AI.");
        }

        var session = new CourseChatSession(
            request.AccountId,
            request.CourseId,
            request.Title);

        await _courseChatSessionRepository.AddSessionAsync(
            session,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CourseChatSessionDto
        {
            Id = session.Id,
            AccountId = session.AccountId,
            CourseId = session.CourseId,
            Title = session.Title,
            CreatedAt = session.CreatedAt
        };
    }
}