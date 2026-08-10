using AILA.Domain.Common;
using AILA.Domain.Enums;
using System;

namespace AILA.Domain.Entities
{
    public class AdminActivityLog : BaseEntity
    {
        public Guid AdminId { get; private set; }

        public AdminAction Action { get; private set; }

        public string? Description { get; private set; }

        public virtual User Admin { get; private set; } = null!;


        private AdminActivityLog() { }


        public AdminActivityLog(
            Guid adminId,
            AdminAction action,
            string? description = null)
        {
            if (!Enum.IsDefined(typeof(AdminAction), action))
                throw new ArgumentException("Admin action không hợp lệ.", nameof(action));
            AdminId = adminId;
            Action = action;
            Description = description;
        }
    }
}