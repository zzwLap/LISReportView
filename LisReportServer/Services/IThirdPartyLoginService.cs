using LisReportServer.Models;

namespace LisReportServer.Services
{
    /// <summary>
    /// 第三方登录服务接口
    /// </summary>
    public interface IThirdPartyLoginService
    {
        /// <summary>
        /// 通过第三方API验证用户凭据
        /// </summary>
        /// <param name="hospitalName">医院名称</param>
        /// <param name="username">用户名</param>
        /// <param name="password">密码</param>
        /// <returns>登录结果（成功/失败及令牌信息）</returns>
        Task<ThirdPartyLoginResult> AuthenticateAsync(string hospitalName, string username, string password);

        /// <summary>
        /// 测试第三方API连接
        /// </summary>
        /// <param name="hospitalName">医院名称</param>
        /// <returns>连接是否成功</returns>
        Task<bool> TestConnectionAsync(string hospitalName);
    }

    /// <summary>
    /// 第三方登录结果
    /// </summary>
    public class ThirdPartyLoginResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
    }
}
