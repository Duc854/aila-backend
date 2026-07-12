namespace AILA.Application.Common.Interfaces
{
    public class GoogleTokenPayload
    {
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? GoogleId { get; set; }
        public string? Picture { get; set; }
    }

    public interface IGoogleAuthService
    {
        Task<GoogleTokenPayload?> VerifyGoogleTokenAsync(string idToken);
        Task<string?> ExchangeCodeForIdTokenAsync(string authorizationCode, string redirectUri, CancellationToken cancellationToken);
    }
}
