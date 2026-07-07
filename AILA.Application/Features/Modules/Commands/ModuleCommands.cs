using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Modules.Dtos;
using AILA.Domain.Entities;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Modules.Commands
{
    // ══════════════════════════════════════════════════════════════════════════
    // CREATE MODULE
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Expert tạo một Chương mới trong Khóa học của mình.
    /// ExpertId dùng để xác minh quyền sở hữu Course.
    /// </summary>
    public record CreateModuleCommand(
        Guid    CourseId,
        Guid    ExpertId,
        string  Title,
        string? Description,
        int     OrderIndex
    ) : IRequest<ResponseDto<ModuleDto>>;

    public class CreateModuleCommandHandler
        : IRequestHandler<CreateModuleCommand, ResponseDto<ModuleDto>>
    {
        private readonly IUnitOfWork _uow;

        public CreateModuleCommandHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ResponseDto<ModuleDto>> Handle(
            CreateModuleCommand request,
            CancellationToken   ct)
        {
            // 1. Kiểm tra Course tồn tại và thuộc về Expert này
            var course = await _uow.Courses.GetByIdAsync(request.CourseId);
            if (course == null)
                return ResponseDto<ModuleDto>.FailResult("COURSE_NOT_FOUND", "Không tìm thấy khóa học.");

            if (course.ExpertId != request.ExpertId)
                return ResponseDto<ModuleDto>.FailResult("FORBIDDEN", "Bạn không có quyền thêm chương vào khóa học này.");

            // 2. Tạo Module mới theo DDD constructor (validation nằm trong Entity)
            var module = new Module(
                courseId:    request.CourseId,
                title:       request.Title,
                orderIndex:  request.OrderIndex,
                description: request.Description);

            await _uow.Modules.AddAsync(module);
            await _uow.SaveChangesAsync(ct);

            return ResponseDto<ModuleDto>.SuccessResult(MapToDto(module));
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // UPDATE MODULE
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>Expert cập nhật tiêu đề và mô tả của một Chương học.</summary>
    public record UpdateModuleCommand(
        Guid    ModuleId,
        Guid    ExpertId,
        string  Title,
        string? Description
    ) : IRequest<ResponseDto<ModuleDto>>;

    public class UpdateModuleCommandHandler
        : IRequestHandler<UpdateModuleCommand, ResponseDto<ModuleDto>>
    {
        private readonly IUnitOfWork _uow;

        public UpdateModuleCommandHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ResponseDto<ModuleDto>> Handle(
            UpdateModuleCommand request,
            CancellationToken   ct)
        {
            // Lấy Module kèm Course cha để kiểm tra quyền
            var module = await _uow.Modules.GetWithCourseAsync(request.ModuleId);
            if (module == null)
                return ResponseDto<ModuleDto>.FailResult("MODULE_NOT_FOUND", "Không tìm thấy chương học.");

            if (module.Course.ExpertId != request.ExpertId)
                return ResponseDto<ModuleDto>.FailResult("FORBIDDEN", "Bạn không có quyền chỉnh sửa chương học này.");

            // Gọi Domain method — validation nằm trong Entity
            module.UpdateInfo(request.Title, request.Description);
            await _uow.SaveChangesAsync(ct);

            return ResponseDto<ModuleDto>.SuccessResult(MapToDto(module));
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // DELETE MODULE
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>Expert xóa một Chương học (kéo theo toàn bộ Material bên trong - Cascade).</summary>
    public record DeleteModuleCommand(Guid ModuleId, Guid ExpertId)
        : IRequest<ResponseDto<object>>;

    public class DeleteModuleCommandHandler
        : IRequestHandler<DeleteModuleCommand, ResponseDto<object>>
    {
        private readonly IUnitOfWork _uow;

        public DeleteModuleCommandHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ResponseDto<object>> Handle(
            DeleteModuleCommand request,
            CancellationToken   ct)
        {
            var module = await _uow.Modules.GetWithCourseAsync(request.ModuleId);
            if (module == null)
                return ResponseDto<object>.FailResult("MODULE_NOT_FOUND", "Không tìm thấy chương học.");

            if (module.Course.ExpertId != request.ExpertId)
                return ResponseDto<object>.FailResult("FORBIDDEN", "Bạn không có quyền xóa chương học này.");

            _uow.Modules.Remove(module);
            await _uow.SaveChangesAsync(ct);

            return ResponseDto<object>.SuccessResult(null!);
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // PUBLISH / UNPUBLISH MODULE
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>Expert công khai hoặc ẩn một Chương học.</summary>
    public record SetModulePublishCommand(Guid ModuleId, Guid ExpertId, bool Publish)
        : IRequest<ResponseDto<ModuleDto>>;

    public class SetModulePublishCommandHandler
        : IRequestHandler<SetModulePublishCommand, ResponseDto<ModuleDto>>
    {
        private readonly IUnitOfWork _uow;

        public SetModulePublishCommandHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ResponseDto<ModuleDto>> Handle(
            SetModulePublishCommand request,
            CancellationToken       ct)
        {
            var module = await _uow.Modules.GetWithCourseAsync(request.ModuleId);
            if (module == null)
                return ResponseDto<ModuleDto>.FailResult("MODULE_NOT_FOUND", "Không tìm thấy chương học.");

            if (module.Course.ExpertId != request.ExpertId)
                return ResponseDto<ModuleDto>.FailResult("FORBIDDEN", "Bạn không có quyền thay đổi trạng thái chương học này.");

            if (request.Publish) module.Publish();
            else                 module.Unpublish();

            await _uow.SaveChangesAsync(ct);

            return ResponseDto<ModuleDto>.SuccessResult(MapToDto(module));
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // REORDER MODULES
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>Expert kéo thả sắp xếp lại thứ tự các Chương trong Khóa học.</summary>
    public record ReorderModulesCommand(
        Guid CourseId,
        Guid ExpertId,
        List<ModuleOrderItem> Items
    ) : IRequest<ResponseDto<object>>;

    public class ReorderModulesCommandHandler
        : IRequestHandler<ReorderModulesCommand, ResponseDto<object>>
    {
        private readonly IUnitOfWork _uow;

        public ReorderModulesCommandHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ResponseDto<object>> Handle(
            ReorderModulesCommand request,
            CancellationToken     ct)
        {
            // Xác minh quyền sở hữu Course
            var course = await _uow.Courses.GetByIdAsync(request.CourseId);
            if (course == null)
                return ResponseDto<object>.FailResult("COURSE_NOT_FOUND", "Không tìm thấy khóa học.");

            if (course.ExpertId != request.ExpertId)
                return ResponseDto<object>.FailResult("FORBIDDEN", "Bạn không có quyền sắp xếp chương của khóa học này.");

            // Lấy toàn bộ Module của Course
            var modules = await _uow.Modules.GetByCourseIdAsync(request.CourseId, ct);
            var moduleMap = modules.ToDictionary(m => m.Id);

            // Cập nhật OrderIndex theo danh sách mới
            foreach (var item in request.Items)
            {
                if (moduleMap.TryGetValue(item.ModuleId, out var module))
                    module.ChangeOrder(item.NewOrderIndex);
            }

            await _uow.SaveChangesAsync(ct);

            return ResponseDto<object>.SuccessResult(null!);
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // SHARED MAPPER (dùng nội bộ trong file này)
    // ══════════════════════════════════════════════════════════════════════════

    internal static class ModuleMapper
    {
        internal static ModuleDto MapToDto(Module m) => new()
        {
            Id            = m.Id,
            CourseId      = m.CourseId,
            Title         = m.Title,
            Description   = m.Description,
            OrderIndex    = m.OrderIndex,
            IsPublished   = m.IsPublished,
            CreatedAt     = m.CreatedAt,
            UpdatedAt     = m.UpdatedAt,
            MaterialCount = m.Materials.Count,
        };
    }

    // Hàm helper dùng trong handlers cùng file
    file static ModuleDto MapToDto(Module m) => ModuleMapper.MapToDto(m);
}
