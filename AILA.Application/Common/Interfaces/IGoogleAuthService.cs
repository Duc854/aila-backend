namespace AILA.Application.Common.Interfaces
{
    public class GoogleTokenPayload
    {
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public interface IGoogleAuthService
    {
        Task<GoogleTokenPayload> VerifyGoogleTokenAsync(string idToken);
    }
}
