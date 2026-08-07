using AILA.Domain.Common;
using System;

namespace AILA.Domain.Entities
{
    public class UserViolationRecord : BaseEntity
    {
        public Guid UserId { get; private set; }
        public Guid? AttemptId { get; private set; }
        public string ViolationType { get; private set; } = string.Empty;
        public string PolicyName { get; private set; } = string.Empty;
        public string Reason { get; private set; } = string.Empty;
        public string Severity { get; private set; } = "Medium";

        // EF Core constructor
        private UserViolationRecord() { }

        public UserViolationRecord(
            Guid userId,
            string violationType,
            string policyName,
            string reason,
            Guid? attemptId = null,
            string severity = "Medium")
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("UserId không được để trống.", nameof(userId));

            Id = Guid.NewGuid();
            UserId = userId;
            AttemptId = attemptId;
            ViolationType = violationType ?? "Violation";
            PolicyName = policyName ?? "GeneralPolicy";
            Reason = reason ?? string.Empty;
            Severity = severity ?? "Medium";
        }
    }
}
