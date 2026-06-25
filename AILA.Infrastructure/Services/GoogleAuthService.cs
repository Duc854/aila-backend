using AILA.Application.Common.Interfaces;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace AILA.Infrastructure.Services
{
    public class GoogleAuthService : IGoogleAuthService
    {
        private readonly IConfiguration _configuration;

        public GoogleAuthService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<GoogleUserPayload?> VerifyGoogleTokenAsync(string idToken)
        {
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings()
                {
                    Audience = new[] { _configuration["Authentication:Google:ClientId"] } // Assuming this config path
                };

                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
                return new GoogleUserPayload 
                {
                    Email = payload.Email,
                    Name = payload.Name,
                    Subject = payload.Subject,
                    Picture = payload.Picture
                };
            }
            catch
            {
                return null;
            }
        }
    }
}
