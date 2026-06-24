using AILA.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Common.Interfaces.Repositories
{
    public interface ICourseRepository : IGenericRepository<Course>
    {
        // HÀM GIẢI QUYẾT BÀI TOÁN LẤY CẤU TRÚC PHÂN CẤP CỦA BẠN:
        // Lấy chi tiết Khóa học gồm toàn bộ Modules, Materials, Video và Document đi kèm.
        Task<Course?> GetCourseWithFullContentAsync(Guid courseId);
    }
}
