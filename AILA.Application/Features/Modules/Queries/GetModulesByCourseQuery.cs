using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Modules.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Modules.Queries
{
    /// <summary>
    /// Lấy danh sách tất cả Chương học của một Khóa học.
    /// ExpertId dùng để xác minh quyền — chỉ Expert sở hữu mới được xem toàn bộ (kể cả chưa publish).
    /// </summary>
    public record GetModulesByCourseQuery(Guid CourseId, Guid ExpertId)
        : IRequest<ResponseDto<List<ModuleDto>>>;

    public class GetModulesByCourseQueryHandler
        : IRequestHandler<GetModulesByCourseQuery, ResponseDto<List<ModuleDto>>>
    {
        private readonly IUnitOfWork _uow;

        public GetModulesByCourseQueryHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ResponseDto<List<ModuleDto>>> Handle(
            GetModulesByCourseQuery request,
            CancellationToken       ct)
        {
            // Xác minh Course tồn tại và thuộc về Expert này
            var course = await _uow.Courses.GetByIdAsync(request.CourseId);
            if (course == null)
                return ResponseDto<List<ModuleDto>>.FailResult(
                    "COURSE_NOT_FOUND", "Không tìm thấy khóa học.");

            if (course.ExpertId != request.ExpertId)
                return ResponseDto<List<ModuleDto>>.FailResult(
                    "FORBIDDEN", "Bạn không có quyền xem danh sách chương của khóa học này.");

            var modules = await _uow.Modules.GetByCourseIdAsync(request.CourseId, ct);

            var dtos = modules.Select(m => new ModuleDto
            {
                Id            = m.Id,
                CourseId      = m.CourseId,
                Title         = m.Title,
                Description   = m.Description,
                OrderIndex    = m.OrderIndex,
                CreatedAt     = m.CreatedAt,
                UpdatedAt     = m.UpdatedAt,
                MaterialCount = m.Materials.Count,
            }).ToList();

            return ResponseDto<List<ModuleDto>>.SuccessResult(dtos);
        }
    }
}
