using AILA.Domain.Common;
using System;

namespace AILA.Domain.Entities
{
    public class UserViolationRecord : BaseEntity
    {
        public Guid UserId { get; private set; }
        public string ViolationType { get; private set; } = string.Empty;
        public string PolicyName { get; private set; } = string.Empty;
        public string Reason { get; private set; } = string.Empty;
        public string ViolatingPrompt { get; private set; } = string.Empty;

        // Navigation property
        public virtual User User { get; private set; } = null!;

        // EF Core constructor
        private UserViolationRecord() { }

        public UserViolationRecord(
            Guid userId,
            string violationType,
            string policyName,
            string reason,
            string violatingPrompt)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("UserId không được để trống.", nameof(userId));

            Id = Guid.NewGuid();
            UserId = userId;
            ViolationType = violationType ?? "Violation";
            PolicyName = policyName ?? "GeneralPolicy";
            Reason = reason ?? string.Empty;
            ViolatingPrompt = violatingPrompt ?? string.Empty;
        }
    }
}
