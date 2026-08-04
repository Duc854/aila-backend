using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Authentication.Dtos;
using AILA.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.Authentication.Commands.RefreshToken
{
    public class RefreshTokenCommandHandler
        : IRequestHandler<RefreshTokenCommand, LoginResponseDto>
    {

        private readonly IUnitOfWork _uow;
        private readonly ITokenProvider _tokenProvider;


        public RefreshTokenCommandHandler(
            IUnitOfWork uow,
            ITokenProvider tokenProvider)
        {
            _uow = uow;
            _tokenProvider = tokenProvider;
        }



        public async Task<LoginResponseDto> Handle(
            RefreshTokenCommand request,
            CancellationToken cancellationToken)
        {

            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                throw new UnauthorizedAccessException(
                    "Refresh Token không hợp lệ."
                );


            // 1. Hash token client gửi lên
            var refreshTokenHash =
                _tokenProvider.HashToken(
                    request.RefreshToken
                );



            // 2. Tìm token trong DB
            var storedToken =
                await _uow.UserTokens
                    .GetByRefreshTokenHashAsync(
                        refreshTokenHash
                    );



            if (storedToken is null)
            {
                throw new UnauthorizedAccessException(
                    "Refresh Token không tồn tại."
                );
            }



            // 3. Check token còn hợp lệ
            if (!storedToken.IsValid())
            {
                throw new UnauthorizedAccessException(
                    "Refresh Token đã hết hạn hoặc bị thu hồi."
                );
            }



            // 4. Lấy User
            var user =
                await _uow.Users.GetByIdAsync(
                    storedToken.UserId
                );


            if (user is null || !user.IsActive)
            {
                throw new UnauthorizedAccessException(
                    "User không hợp lệ."
                );
            }



            // 5. Generate token mới
            var newAccessToken =
                _tokenProvider.GenerateAccessToken(user);


            var newRefreshToken =
                _tokenProvider.GenerateRefreshToken();


            var newRefreshTokenHash =
                _tokenProvider.HashToken(
                    newRefreshToken
                );



            // 6. Revoke token cũ
            storedToken.Revoke();



            // 7. Save token mới
            var newUserToken = new UserToken(
                user.Id,
                newRefreshTokenHash,
                DateTime.UtcNow.AddDays(7)
            );


            _uow.UserTokens.Add(newUserToken);


            await _uow.SaveChangesAsync(
                cancellationToken
            );



            return new LoginResponseDto
            {
                AccessToken = newAccessToken,

                RefreshToken = newRefreshToken,

                Role = user.Role.ToString(),

                UserId = user.Id,

                FullName = user.FullName,

                Email = user.Email
            };
        }
    }
}
