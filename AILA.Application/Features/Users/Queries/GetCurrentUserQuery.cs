using AILA.Application.Features.Auth.DTOs;
using MediatR;
using System;

namespace AILA.Application.Features.Users.Queries
{
    public class GetCurrentUserQuery : IRequest<UserInfoDto>
    {
        public Guid UserId { get; set; }
    }
}
