using System.Text.Json;

namespace LisReportServer.Services
{
    public interface ICookieService
    {
        void SetRememberMeCookie(string hospitalName, string username, bool rememberMe);
        (string hospitalName, string username)? GetRememberMeCookie();
        void ClearRememberMeCookie();
    }

    public class CookieService : ICookieService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string REMEMBER_ME_COOKIE_NAME = "RememberMeCredentials";
        private const int REMEMBER_ME_COOKIE_DAYS = 30;

        public CookieService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public void SetRememberMeCookie(string hospitalName, string username, bool rememberMe)
        {
            if (!rememberMe)
            {
                ClearRememberMeCookie();
                return;
            }

            var credentials = new { HospitalName = hospitalName, Username = username };
            var json = JsonSerializer.Serialize(credentials);
            var encodedJson = System.Web.HttpUtility.UrlEncode(json);

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = false, // 在生产环境中应设置为true（需要HTTPS）
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(REMEMBER_ME_COOKIE_DAYS)
            };

            _httpContextAccessor.HttpContext?.Response.Cookies.Append(REMEMBER_ME_COOKIE_NAME, encodedJson, cookieOptions);
        }

        public (string hospitalName, string username)? GetRememberMeCookie()
        {
            var cookieValue = _httpContextAccessor.HttpContext?.Request.Cookies[REMEMBER_ME_COOKIE_NAME];
            
            if (string.IsNullOrEmpty(cookieValue))
                return null;

            try
            {
                var decodedJson = System.Web.HttpUtility.UrlDecode(cookieValue);
                var credentials = JsonSerializer.Deserialize<CredentialsData>(decodedJson);
                
                if (credentials != null)
                {
                    return (credentials.HospitalName, credentials.Username);
                }
            }
            catch
            {
                // 如果反序列化失败，清除cookie
                ClearRememberMeCookie();
            }

            return null;
        }

        public void ClearRememberMeCookie()
        {
            _httpContextAccessor.HttpContext?.Response.Cookies.Delete(REMEMBER_ME_COOKIE_NAME);
        }

        private class CredentialsData
        {
            public string HospitalName { get; set; } = string.Empty;
            public string Username { get; set; } = string.Empty;
        }
    }
}