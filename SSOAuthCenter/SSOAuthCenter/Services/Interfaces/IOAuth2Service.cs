using SSOAuthCenter.Models;

namespace SSOAuthCenter.Services.Interfaces
{
    public interface IOAuth2Service
    {
        Task<string> GenerateAuthorizationCodeAsync(string clientId, string redirectUri, string scope, int userId);
        Task<(bool isValid, int userId)> ValidateAuthorizationCodeAsync(string code, string clientId);
        Task<(string accessToken, string refreshToken, int expiresIn)> GenerateAccessAndRefreshTokensAsync(string clientId, int userId, string scope);
        Task<(bool isValid, int userId)> ValidateAccessTokenAsync(string token);
        Task<(bool isValid, int userId)> ValidateRefreshTokenAsync(string token);
        Task<(string newAccessToken, string newRefreshToken, int expiresIn)> RefreshAccessTokenAsync(string refreshToken);
        Task<bool> RevokeTokenAsync(string token);
        Task<ClientApplication?> GetClientApplicationAsync(string clientId);
        Task<bool> IsValidRedirectUriAsync(string clientId, string redirectUri);
    }
}