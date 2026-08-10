using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.Authentication.Commands.Logout
{
    public class LogoutCommand : IRequest<bool>
    {
        public string RefreshToken { get; }

        public string Jti { get; }

        public DateTime AccessTokenExpiredAt { get; }


        public LogoutCommand(
            string refreshToken,
            string jti,
            DateTime accessTokenExpiredAt)
        {
            RefreshToken = refreshToken;
            Jti = jti;
            AccessTokenExpiredAt = accessTokenExpiredAt;
        }
    }
}
