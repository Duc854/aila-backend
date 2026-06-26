using Google.Apis.Auth;
using AILA.Application.Common.Interfaces;

namespace AILA.Infrastructure.Services
{
    public class GoogleAuthService : IGoogleAuthService
    {
        public async Task<GoogleTokenPayload> VerifyGoogleTokenAsync(string idToken)
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken);

            return new GoogleTokenPayload
            {
                Email = payload.Email,
                Name = payload.Name
            };
        }
    }
}