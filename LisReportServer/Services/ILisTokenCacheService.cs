namespace LisReportServer.Services
{
    /// <summary>
    /// LIS登录Token缓存服务接口
    /// </summary>
    public interface ILisTokenCacheService
    {
        /// <summary>
        /// 存储用户的LIS登录Token
        /// </summary>
        /// <param name="hospitalName">医院名称</param>
        /// <param name="username">用户名</param>
        /// <param name="accessToken">访问令牌</param>
        /// <param name="refreshToken">刷新令牌</param>
        /// <param name="expirationMinutes">过期时间（分钟）</param>
        Task SetTokenAsync(string hospitalName, string username, string accessToken, string? refreshToken, int expirationMinutes = 30);

        /// <summary>
        /// 获取用户的LIS登录Token
        /// </summary>
        /// <param name="hospitalName">医院名称</param>
        /// <param name="username">用户名</param>
        /// <returns>Token信息</returns>
        Task<LisTokenInfo?> GetTokenAsync(string hospitalName, string username);

        /// <summary>
        /// 清除用户的LIS登录Token
        /// </summary>
        /// <param name="hospitalName">医院名称</param>
        /// <param name="username">用户名</param>
        Task RemoveTokenAsync(string hospitalName, string username);

        /// <summary>
        /// 清除所有过期的Token
        /// </summary>
        Task RemoveExpiredTokensAsync();
    }

    /// <summary>
    /// LIS Token信息
    /// </summary>
    public class LisTokenInfo
    {
        public string AccessToken { get; set; } = string.Empty;
        public string? RefreshToken { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
