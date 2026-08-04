using AILA.Application.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Infrastructure.Security
{
    public class JwtAccessTokenService : IAccessTokenService{

        public TokenInfo GetTokenInfo(string accessToken){
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException(
                    "Access Token không được để trống.",
                    nameof(accessToken));

            var handler =
                new JwtSecurityTokenHandler();

            JwtSecurityToken token;
            try
            {
                token =
                    handler.ReadJwtToken(
                        accessToken);
            }
            catch
            {
                throw new UnauthorizedAccessException(
                    "Access Token không hợp lệ.");
            }
            var jti =
                token.Claims
                    .FirstOrDefault(
                        x => x.Type == JwtRegisteredClaimNames.Jti)
                    ?.Value;
            if (string.IsNullOrWhiteSpace(jti))
            {
                throw new UnauthorizedAccessException(
                    "Access Token không chứa JTI.");
            }
            var exp =
                token.Claims
                    .FirstOrDefault(
                        x => x.Type == JwtRegisteredClaimNames.Exp)
                    ?.Value;
            if (!long.TryParse(
                    exp,
                    out var seconds))
            {
                throw new UnauthorizedAccessException(
                    "Access Token không chứa thời gian hết hạn.");
            }
            var expiredAt =
                DateTimeOffset
                    .FromUnixTimeSeconds(seconds)
                    .UtcDateTime;
            return new TokenInfo
            {
                Jti = jti,
                ExpiredAt = expiredAt
            };
        }
    }
}
