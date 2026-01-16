using Microsoft.EntityFrameworkCore;
using SSOAuthCenter.Data;
using SSOAuthCenter.Models;
using SSOAuthCenter.Services.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace SSOAuthCenter.Services
{
    public class OAuth2Service : IOAuth2Service
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OAuth2Service> _logger;

        public OAuth2Service(ApplicationDbContext context, ILogger<OAuth2Service> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<string> GenerateAuthorizationCodeAsync(string clientId, string redirectUri, string scope, int userId)
        {
            // 验证客户端和重定向URI
            var client = await GetClientApplicationAsync(clientId);
            if (client == null || !await IsValidRedirectUriAsync(clientId, redirectUri))
            {
                _logger.LogWarning("Invalid client or redirect URI for authorization code generation: {ClientId}, {RedirectUri}", clientId, redirectUri);
                return string.Empty;
            }

            // 生成授权码
            var code = GenerateSecureToken(32); // 生成32字节的随机码
            var expiresAt = DateTime.UtcNow.AddMinutes(5); // 授权码有效期5分钟

            var authToken = new AuthToken
            {
                TokenValue = code,
                UserId = userId,
                TokenType = "AuthorizationCode",
                ClientId = clientId,
                Scopes = scope,
                ExpiresAt = expiresAt,
                CreatedAt = DateTime.UtcNow
            };

            _context.AuthTokens.Add(authToken);
            await _context.SaveChangesAsync();

            return code;
        }

        public async Task<(bool isValid, int userId)> ValidateAuthorizationCodeAsync(string code, string clientId)
        {
            var token = await _context.AuthTokens
                .FirstOrDefaultAsync(t => t.TokenValue == code && t.TokenType == "AuthorizationCode");

            if (token == null || token.IsRevoked || token.ExpiresAt < DateTime.UtcNow)
            {
                return (false, 0);
            }

            // 验证客户端ID
            if (token.ClientId != clientId)
            {
                _logger.LogWarning("Authorization code client mismatch: expected {Expected}, got {Actual}", token.ClientId, clientId);
                return (false, 0);
            }

            // 标记授权码为已使用（一次性使用）
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
            _context.AuthTokens.Update(token);
            await _context.SaveChangesAsync();

            return (true, token.UserId);
        }

        public async Task<(string accessToken, string refreshToken, int expiresIn)> GenerateAccessAndRefreshTokensAsync(string clientId, int userId, string scope)
        {
            // 验证客户端
            var client = await GetClientApplicationAsync(clientId);
            if (client == null)
            {
                _logger.LogWarning("Invalid client for token generation: {ClientId}", clientId);
                return (string.Empty, string.Empty, 0);
            }

            // 生成访问令牌
            var accessToken = GenerateSecureToken(32);
            var refreshToken = GenerateSecureToken(32);
            
            var accessExpiresAt = DateTime.UtcNow.AddHours(1); // 访问令牌1小时过期
            var refreshExpiresAt = DateTime.UtcNow.AddDays(30); // 刷新令牌30天过期

            // 创建访问令牌
            var accessAuthToken = new AuthToken
            {
                TokenValue = accessToken,
                UserId = userId,
                TokenType = "AccessToken",
                ClientId = clientId,
                Scopes = scope,
                ExpiresAt = accessExpiresAt,
                CreatedAt = DateTime.UtcNow
            };

            // 创建刷新令牌
            var refreshAuthToken = new AuthToken
            {
                TokenValue = refreshToken,
                UserId = userId,
                TokenType = "RefreshToken",
                ClientId = clientId,
                Scopes = scope,
                ExpiresAt = refreshExpiresAt,
                CreatedAt = DateTime.UtcNow
            };

            _context.AuthTokens.AddRange(accessAuthToken, refreshAuthToken);
            await _context.SaveChangesAsync();

            return (accessToken, refreshToken, 3600); // 1小时 = 3600秒
        }

        public async Task<(bool isValid, int userId)> ValidateAccessTokenAsync(string token)
        {
            var authToken = await _context.AuthTokens
                .FirstOrDefaultAsync(t => t.TokenValue == token && t.TokenType == "AccessToken");

            if (authToken == null || authToken.IsRevoked || authToken.ExpiresAt < DateTime.UtcNow)
            {
                return (false, 0);
            }

            return (true, authToken.UserId);
        }

        public async Task<(bool isValid, int userId)> ValidateRefreshTokenAsync(string token)
        {
            var authToken = await _context.AuthTokens
                .FirstOrDefaultAsync(t => t.TokenValue == token && t.TokenType == "RefreshToken");

            if (authToken == null || authToken.IsRevoked || authToken.ExpiresAt < DateTime.UtcNow)
            {
                return (false, 0);
            }

            return (true, authToken.UserId);
        }

        public async Task<(string newAccessToken, string newRefreshToken, int expiresIn)> RefreshAccessTokenAsync(string refreshToken)
        {
            var refreshTokenEntity = await _context.AuthTokens
                .FirstOrDefaultAsync(t => t.TokenValue == refreshToken && t.TokenType == "RefreshToken");

            if (refreshTokenEntity == null || refreshTokenEntity.IsRevoked || refreshTokenEntity.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Invalid or expired refresh token: {Token}", refreshToken);
                return (string.Empty, string.Empty, 0);
            }

            // 生成新的访问令牌
            var newAccessToken = GenerateSecureToken(32);
            var newRefreshToken = GenerateSecureToken(32); // 也可以选择使用相同的刷新令牌

            var accessExpiresAt = DateTime.UtcNow.AddHours(1); // 新的访问令牌1小时过期
            var refreshExpiresAt = DateTime.UtcNow.AddDays(30); // 新的刷新令牌30天过期

            // 创建新的访问令牌
            var newAccessAuthToken = new AuthToken
            {
                TokenValue = newAccessToken,
                UserId = refreshTokenEntity.UserId,
                TokenType = "AccessToken",
                ClientId = refreshTokenEntity.ClientId,
                Scopes = refreshTokenEntity.Scopes,
                ExpiresAt = accessExpiresAt,
                CreatedAt = DateTime.UtcNow
            };

            // 创建新的刷新令牌
            var newRefreshAuthToken = new AuthToken
            {
                TokenValue = newRefreshToken,
                UserId = refreshTokenEntity.UserId,
                TokenType = "RefreshToken",
                ClientId = refreshTokenEntity.ClientId,
                Scopes = refreshTokenEntity.Scopes,
                ExpiresAt = refreshExpiresAt,
                CreatedAt = DateTime.UtcNow
            };

            // 撤销旧的刷新令牌
            refreshTokenEntity.IsRevoked = true;
            refreshTokenEntity.RevokedAt = DateTime.UtcNow;
            _context.AuthTokens.Update(refreshTokenEntity);

            // 添加新的令牌
            _context.AuthTokens.AddRange(newAccessAuthToken, newRefreshAuthToken);
            await _context.SaveChangesAsync();

            return (newAccessToken, newRefreshToken, 3600);
        }

        public async Task<bool> RevokeTokenAsync(string token)
        {
            var authToken = await _context.AuthTokens
                .FirstOrDefaultAsync(t => t.TokenValue == token);

            if (authToken == null)
            {
                return false;
            }

            authToken.IsRevoked = true;
            authToken.RevokedAt = DateTime.UtcNow;
            _context.AuthTokens.Update(authToken);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<ClientApplication?> GetClientApplicationAsync(string clientId)
        {
            return await _context.ClientApplications
                .FirstOrDefaultAsync(c => c.ClientId == clientId && c.IsActive);
        }

        public async Task<bool> IsValidRedirectUriAsync(string clientId, string redirectUri)
        {
            var client = await GetClientApplicationAsync(clientId);
            return client != null && client.RedirectUri == redirectUri;
        }

        private string GenerateSecureToken(int length)
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var bytes = new byte[length];
                rng.GetBytes(bytes);
                return Convert.ToBase64String(bytes)
                    .Replace("+", "")
                    .Replace("/", "")
                    .Replace("=", "");
            }
        }
    }
}