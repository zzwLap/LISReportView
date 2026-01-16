using Microsoft.AspNetCore.Mvc;
using SSOAuthCenter.Services.Interfaces;
using System.Security.Claims;

namespace SSOAuthCenter.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OAuthController : ControllerBase
    {
        private readonly IOAuth2Service _oauthService;
        private readonly IAuthService _authService;
        private readonly ILogger<OAuthController> _logger;

        public OAuthController(IOAuth2Service oauthService, IAuthService authService, ILogger<OAuthController> logger)
        {
            _oauthService = oauthService;
            _authService = authService;
            _logger = logger;
        }

        // OAuth2 授权码流程第一步：获取授权码
        [HttpGet("authorize")]
        public async Task<IActionResult> Authorize([FromQuery] string client_id, [FromQuery] string redirect_uri, 
            [FromQuery] string response_type, [FromQuery] string scope, [FromQuery] string state)
        {
            // 验证必需参数
            if (string.IsNullOrEmpty(client_id) || string.IsNullOrEmpty(redirect_uri) || 
                string.IsNullOrEmpty(response_type) || response_type != "code")
            {
                return BadRequest(new { error = "invalid_request", error_description = "Missing or invalid parameters" });
            }

            // 验证客户端应用
            var client = await _oauthService.GetClientApplicationAsync(client_id);
            if (client == null)
            {
                return BadRequest(new { error = "invalid_client", error_description = "Invalid client_id" });
            }

            // 验证重定向URI
            if (!await _oauthService.IsValidRedirectUriAsync(client_id, redirect_uri))
            {
                return BadRequest(new { error = "invalid_request", error_description = "Invalid redirect_uri" });
            }

            // 检查用户是否已登录
            int userId = 0;
            if (User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdClaim, out var parsedUserId))
                {
                    userId = parsedUserId;
                }
            }

            // 如果用户未登录，重定向到登录页面
            if (userId == 0)
            {
                var encodedRedirectUri = Uri.EscapeDataString($"{Request.Scheme}://{Request.Host}{Request.Path}?{Request.QueryString}");
                return Redirect($"/Login?ReturnUrl={encodedRedirectUri}");
            }

            // 生成授权码
            var code = await _oauthService.GenerateAuthorizationCodeAsync(client_id, redirect_uri, scope, userId);
            if (string.IsNullOrEmpty(code))
            {
                return BadRequest(new { error = "server_error", error_description = "Failed to generate authorization code" });
            }

            // 构建重定向URL
            var redirectUrl = $"{redirect_uri}?code={code}";
            if (!string.IsNullOrEmpty(state))
            {
                redirectUrl += $"&state={state}";
            }

            return Redirect(redirectUrl);
        }

        // OAuth2 获取访问令牌
        [HttpPost("token")]
        public async Task<IActionResult> Token([FromForm] string grant_type, [FromForm] string? code, 
            [FromForm] string? redirect_uri, [FromForm] string? refresh_token, [FromForm] string client_id, 
            [FromForm] string client_secret)
        {
            if (grant_type == "authorization_code")
            {
                // 授权码模式
                if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(redirect_uri))
                {
                    return BadRequest(new { error = "invalid_request", error_description = "Missing code or redirect_uri" });
                }

                // 验证客户端
                var client = await _oauthService.GetClientApplicationAsync(client_id);
                if (client == null || client.ClientSecret != client_secret)
                {
                    return BadRequest(new { error = "invalid_client", error_description = "Invalid client credentials" });
                }

                // 验证授权码
                var (isValid, userId) = await _oauthService.ValidateAuthorizationCodeAsync(code, client_id);
                if (!isValid)
                {
                    return BadRequest(new { error = "invalid_grant", error_description = "Invalid authorization code" });
                }

                // 生成访问令牌和刷新令牌
                var (accessToken, refreshToken, expiresIn) = await _oauthService.GenerateAccessAndRefreshTokensAsync(client_id, userId, "default");

                if (string.IsNullOrEmpty(accessToken))
                {
                    return BadRequest(new { error = "server_error", error_description = "Failed to generate tokens" });
                }

                return Ok(new
                {
                    access_token = accessToken,
                    refresh_token = refreshToken,
                    token_type = "Bearer",
                    expires_in = expiresIn
                });
            }
            else if (grant_type == "refresh_token")
            {
                // 刷新令牌模式
                if (string.IsNullOrEmpty(refresh_token))
                {
                    return BadRequest(new { error = "invalid_request", error_description = "Missing refresh_token" });
                }

                // 验证客户端
                var client = await _oauthService.GetClientApplicationAsync(client_id);
                if (client == null || client.ClientSecret != client_secret)
                {
                    return BadRequest(new { error = "invalid_client", error_description = "Invalid client credentials" });
                }

                // 刷新访问令牌
                var (newAccessToken, newRefreshToken, expiresIn) = await _oauthService.RefreshAccessTokenAsync(refresh_token);

                if (string.IsNullOrEmpty(newAccessToken))
                {
                    return BadRequest(new { error = "invalid_grant", error_description = "Invalid refresh token" });
                }

                return Ok(new
                {
                    access_token = newAccessToken,
                    refresh_token = newRefreshToken,
                    token_type = "Bearer",
                    expires_in = expiresIn
                });
            }
            else
            {
                return BadRequest(new { error = "unsupported_grant_type", error_description = "Unsupported grant type" });
            }
        }

        // OAuth2 用户信息端点
        [HttpGet("userinfo")]
        public async Task<IActionResult> UserInfo()
        {
            // 验证访问令牌
            var authHeader = Request.Headers.Authorization.FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return Unauthorized(new { error = "invalid_request", error_description = "Missing or invalid Authorization header" });
            }

            var accessToken = authHeader.Substring("Bearer ".Length);
            var (isValid, userId) = await _oauthService.ValidateAccessTokenAsync(accessToken);

            if (!isValid)
            {
                return Unauthorized(new { error = "invalid_token", error_description = "Invalid access token" });
            }

            // 获取用户信息
            var user = await _authService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { error = "user_not_found", error_description = "User not found" });
            }

            // 获取用户角色
            var roles = await _authService.GetUserRolesAsync(userId);
            var roleNames = roles.Select(r => r.Name).ToArray();

            return Ok(new
            {
                sub = user.Id,
                username = user.Username,
                email = user.Email,
                first_name = user.FirstName,
                last_name = user.LastName,
                roles = roleNames,
                created_at = user.CreatedAt
            });
        }
    }
}