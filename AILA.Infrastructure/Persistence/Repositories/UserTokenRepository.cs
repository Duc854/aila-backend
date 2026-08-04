using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Infrastructure.Persistence.Repositories
{
    public class UserTokenRepository
        : IUserTokenRepository
    {
        private readonly ApplicationDbContext _context;


        public UserTokenRepository(
            ApplicationDbContext context)
        {
            _context = context;
        }



        public void Add(UserToken userToken)
        {
            _context.UserTokens.Add(userToken);
        }



        public async Task<UserToken?> GetByRefreshTokenHashAsync(
            string refreshTokenHash)
        {
            return await _context.UserTokens
                .FirstOrDefaultAsync(x =>
                    x.RefreshTokenHash == refreshTokenHash);
        }



        public async Task RevokeAllUserTokensAsync(
            Guid userId)
        {
            var tokens = await _context.UserTokens
                .Where(x =>
                    x.UserId == userId &&
                    !x.IsRevoked)
                .ToListAsync();


            foreach (var token in tokens)
            {
                token.Revoke();
            }
        }
    }
}
