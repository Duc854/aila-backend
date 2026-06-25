using System.Threading.Tasks;

namespace AILA.Application.Common.Interfaces
{
    public interface IGoogleAuthService
    {
        Task<GoogleUserPayload?> VerifyGoogleTokenAsync(string idToken);
    }

    public class GoogleUserPayload
    {
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Picture { get; set; } = string.Empty;
    }
}
