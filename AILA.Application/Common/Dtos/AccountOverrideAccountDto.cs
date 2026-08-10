using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Common.Dtos
{
    public class AccountOverrideAccountDto
    {
        public Guid AccountId { get; set; }

        public string Email { get; set; } = string.Empty;

        public string? FullName { get; set; }

        public string Role { get; set; } = string.Empty;

        public bool HasOverride { get; set; }
    }
}
