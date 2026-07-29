using AILA.Application.Features.Reports.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Reports.Commands.UnlockCourse;

/// <summary>
/// Admin gỡ khoá course để expert có thể publish lại.
/// Không liên kết với report cụ thể — admin có thể unlock bất kỳ lúc nào.
/// </summary>
public sealed record UnlockCourseCommand(Guid CourseId)
    : IRequest<ResponseDto<CourseModerationResponseDto>>;
