using AILA.Application.Features.Authentication.Dtos;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.Authentication.Commands.RefreshToken
{
    public class RefreshTokenCommand
        : IRequest<LoginResponseDto>
    {
        public string RefreshToken { get; set; }


        public RefreshTokenCommand(
            string refreshToken)
        {
            RefreshToken = refreshToken;
        }
    }
}
