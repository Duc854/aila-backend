using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Common.Interfaces
{
    public interface ITokenBlacklistService
    {
        Task BlacklistAsync(
            string jti,
            TimeSpan ttl);

        Task<bool> IsBlacklistedAsync(
            string jti);
    }
}
