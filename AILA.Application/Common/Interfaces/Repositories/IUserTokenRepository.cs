using AILA.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Common.Interfaces.Repositories
{
    public interface IUserTokenRepository
    {
        void Add(UserToken userToken);

        Task<UserToken?> GetByRefreshTokenHashAsync(
            string refreshTokenHash);

        Task RevokeAllUserTokensAsync(
            Guid userId);
    }
}
