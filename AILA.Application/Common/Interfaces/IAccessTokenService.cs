using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Common.Interfaces
{
    public interface IAccessTokenService
    {
        TokenInfo GetTokenInfo(string accessToken);
    }
    public class TokenInfo
    {
        public string Jti { get; set; } = string.Empty;

        public DateTime ExpiredAt { get; set; }
    }
}
