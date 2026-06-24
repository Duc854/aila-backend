using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Domain.Enums
{
    public enum UserRole
    {
        Learner,
        Expert,
        Admin   // Tài khoản quản trị hệ thống (không lưu trong DB, dùng appsettings)
    }
}
