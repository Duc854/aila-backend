using System.Text.Json;
using System.Text.Json.Serialization;
using AILA.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Google.Apis.Auth;
using Shared.Models;

namespace AILA.Infrastructure.Services
{
    public class GoogleAuthService : IGoogleAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly GoogleSettings _googleSettings;
        private readonly ILogger<GoogleAuthService> _logger;

        public GoogleAuthService(HttpClient httpClient, IOptions<GoogleSettings> googleSettings, ILogger<GoogleAuthService> logger)
        {
            _httpClient = httpClient;
            _googleSettings = googleSettings.Value;
            _logger = logger;
        }

        public async Task<GoogleTokenPayload?> VerifyGoogleTokenAsync(string idToken)
        {
            try
            {
                _logger.LogInformation("Verifying Google ID token. tokenLength={TokenLength}", idToken.Length);
                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken);

                _logger.LogInformation("Google ID token verified successfully. email={Email}", payload.Email);

                return new GoogleTokenPayload
                {
                    Email = payload.Email,
                    Name = payload.Name,
                    GoogleId = payload.Subject,
                    Picture = payload.Picture
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Google ID token verification failed.");
                return null;
            }
        }

        public async Task<string?> ExchangeCodeForIdTokenAsync(string authorizationCode, string redirectUri, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Exchanging Google authorization code for token. codeLength={CodeLength}, redirectUri={RedirectUri}", authorizationCode.Length, redirectUri);

            if (string.IsNullOrWhiteSpace(_googleSettings.ClientId)
                || string.IsNullOrWhiteSpace(_googleSettings.ClientSecret))
            {
                throw new InvalidOperationException("GoogleSettings chưa được cấu hình đầy đủ.");
            }

            var effectiveRedirectUri = string.IsNullOrWhiteSpace(redirectUri) ? _googleSettings.RedirectUri : redirectUri;
            if (string.IsNullOrWhiteSpace(effectiveRedirectUri))
            {
                throw new InvalidOperationException("Google redirect URI chưa được cung cấp.");
            }

            var request = new HttpRequestMessage(HttpMethod.Post, "https://oauth2.googleapis.com/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["code"] = authorizationCode,
                    ["client_id"] = _googleSettings.ClientId,
                    ["client_secret"] = _googleSettings.ClientSecret,
                    ["redirect_uri"] = effectiveRedirectUri,
                    ["grant_type"] = "authorization_code"
                })
            };

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogInformation("Google token endpoint response status={StatusCode}, content={Content}", response.StatusCode, content);

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Google token endpoint failed: {(int)response.StatusCode} {response.StatusCode}. Response: {content}");
            var tokenResponse = JsonSerializer.Deserialize<GoogleTokenExchangeResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (tokenResponse == null)
            {
                _logger.LogWarning("Google token response could not be deserialized.");
            }

            return tokenResponse?.IdToken;
        }

        private sealed class GoogleTokenExchangeResponse
        {
            [JsonPropertyName("access_token")]
            public string? AccessToken { get; set; }

            [JsonPropertyName("expires_in")]
            public int? ExpiresIn { get; set; }

            [JsonPropertyName("id_token")]
            public string? IdToken { get; set; }

            [JsonPropertyName("refresh_token")]
            public string? RefreshToken { get; set; }

            [JsonPropertyName("scope")]
            public string? Scope { get; set; }

            [JsonPropertyName("token_type")]
            public string? TokenType { get; set; }
        }
    }
}
