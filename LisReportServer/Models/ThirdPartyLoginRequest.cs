using System.ComponentModel.DataAnnotations;

namespace LisReportServer.Models
{
    /// <summary>
    /// 第三方登录请求模型
    /// </summary>
    public class ThirdPartyLoginRequest
    {
        [Required]
        public string UserName { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// 第三方登录响应模型
    /// </summary>
    public class ThirdPartyLoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }
}
