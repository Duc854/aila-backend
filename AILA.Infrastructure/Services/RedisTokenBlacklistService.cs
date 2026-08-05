using AILA.Application.Common.Interfaces;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Infrastructure.Services
{
    public class RedisTokenBlacklistService : ITokenBlacklistService{

        private readonly IConnectionMultiplexer _redis;

        public RedisTokenBlacklistService(IConnectionMultiplexer redis){
            _redis = redis;
        }

        public async Task BlacklistAsync(string jti,TimeSpan expiry){
           
            var db = _redis.GetDatabase();
            await db.StringSetAsync(GetKey(jti),"revoked",expiry);
        }

        public async Task<bool> IsBlacklistedAsync(string jti){
            
            var db = _redis.GetDatabase();
            return await db.KeyExistsAsync(GetKey(jti));
        }

        private static string GetKey(string jti){

            return $"auth:blacklist:{jti}";
        }
    }
}
