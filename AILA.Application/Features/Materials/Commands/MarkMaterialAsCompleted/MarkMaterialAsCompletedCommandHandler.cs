using AILA.Application.Common.Interfaces;
using AILA.Domain.Entities;
using MediatR;
using Shared.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.Materials.Commands.MarkMaterialAsCompleted
{
    public class MarkMaterialAsCompletedCommandHandler : IRequestHandler<MarkMaterialAsCompletedCommand, ResponseDto<bool>>
    {
        private readonly IUnitOfWork _uow;

        public MarkMaterialAsCompletedCommandHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<ResponseDto<bool>> Handle(MarkMaterialAsCompletedCommand request, CancellationToken cancellationToken)
        {
            // 1. Kiểm tra đăng ký học thông qua EnrollmentRepository chi tiết
            var enrollment = await _uow.Enrollments.GetByCourseAndLearnerAsync(request.CourseId, request.LearnerId, cancellationToken);
            if (enrollment == null)
            {
                return ResponseDto<bool>.FailResult("ENROLLMENT_NOT_FOUND", "Bạn chưa đăng ký tham gia khóa học này.");
            }

            // 2. Kiểm tra tính hợp lệ của học liệu thông qua MaterialRepository chi tiết
            var isMaterialValid = await _uow.Materials.IsMaterialInCourseAsync(request.MaterialId, request.CourseId, cancellationToken);
            if (!isMaterialValid)
            {
                return ResponseDto<bool>.FailResult("MATERIAL_NOT_FOUND", "Học liệu không tồn tại hoặc không thuộc khóa học này.");
            }

            // Mở Transaction để đồng bộ hóa thay đổi đa bảng
            await _uow.BeginTransactionAsync(cancellationToken);

            try
            {
                // 3. Lấy thông tin trạng thái học tập chi tiết qua LearningProgressRepository chi tiết
                var progress = await _uow.LearningProgresses.GetByCompositeKeyAsync(enrollment.Id, request.MaterialId, cancellationToken);

                if (progress == null)
                {
                    // Khởi tạo bản ghi mới qua Constructor Domain nếu chưa từng học bài này
                    progress = new LearningProgress(enrollment.Id, request.MaterialId);
                    await _uow.LearningProgresses.AddAsync(progress, cancellationToken);
                }

                // Nếu bài học đã được đánh dấu hoàn thành từ trước, kết thúc sớm (Idempotent)
                if (progress.IsCompleted)
                {
                    await _uow.RollbackTransactionAsync(cancellationToken);
                    return ResponseDto<bool>.SuccessResult(true);
                }

                // 4. Thực thi logic thay đổi trạng thái bên trong LearningProgress Entity
                progress.Complete();

                // 5. Cập nhật tiến độ khóa học tổng quan qua hàm có sẵn của bạn trong Enrollment.cs
                enrollment.CompleteMaterial();
                _uow.Enrollments.Update(enrollment);

                // 6. Lưu thay đổi và commit Transaction
                await _uow.SaveChangesAsync(cancellationToken);
                await _uow.CommitTransactionAsync(cancellationToken);

                return ResponseDto<bool>.SuccessResult(true);
            }
            catch (Exception)
            {
                await _uow.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
    }
}
