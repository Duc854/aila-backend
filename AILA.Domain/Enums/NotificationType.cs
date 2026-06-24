using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Domain.Enums
{
    public enum NotificationType
    {
        System,        // Thông báo chung từ Admin/Hệ thống
        CourseUpdate,  // Khóa học có bài mới, module mới
        Enrollment,    // Đăng ký học thành công
        Assignment     // Có bài tập mới hoặc được chấm điểm
    }
}
