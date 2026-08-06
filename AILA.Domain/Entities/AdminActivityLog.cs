using AILA.Domain.Common;
using AILA.Domain.Enums;
using System;

namespace AILA.Domain.Entities
{
    public class AdminActivityLog : BaseEntity
    {
        public Guid AdminId { get; private set; }

        public AdminAction Action { get; private set; }

        public string EntityType { get; private set; }

        public Guid? EntityId { get; private set; }

        public string? Description { get; private set; }

        public string? IpAddress { get; private set; }

        public virtual User Admin { get; private set; } = null!;


        private AdminActivityLog() { }


        public AdminActivityLog(
            Guid adminId,
            AdminAction action,
            string entityType,
            Guid? entityId = null,
            string? description = null,
            string? ipAddress = null)
        {
            if (!Enum.IsDefined(typeof(AdminAction), action))
                throw new ArgumentException("Admin action không hợp lệ.", nameof(action));

            if (string.IsNullOrWhiteSpace(entityType))
                throw new ArgumentException("Entity type không được để trống.", nameof(entityType));


            AdminId = adminId;
            Action = action;
            EntityType = entityType.Trim();
            EntityId = entityId;
            Description = description;
            IpAddress = ipAddress;
        }
    }
}