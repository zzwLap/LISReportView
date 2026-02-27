namespace LisReportServer.Models
{
    /// <summary>
    /// 医院API请求配置
    /// </summary>
    public class HospitalApiRequest
    {
        /// <summary>
        /// 请求路径（相对于服务器地址）
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// HTTP方法（GET、POST、PUT、DELETE等）
        /// </summary>
        public string Method { get; set; } = "GET";

        /// <summary>
        /// 请求内容
        /// </summary>
        public object? Content { get; set; }

        /// <summary>
        /// 请求头
        /// </summary>
        public Dictionary<string, string> Headers { get; set; } = new();

        /// <summary>
        /// 查询参数
        /// </summary>
        public Dictionary<string, string?> QueryParameters { get; set; } = new();

        /// <summary>
        /// 超时时间（秒）
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;
    }

    /// <summary>
    /// 医院API响应
    /// </summary>
    public class HospitalApiResponse
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// HTTP状态码
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// 响应内容
        /// </summary>
        public string? Content { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 响应头
        /// </summary>
        public Dictionary<string, string> Headers { get; set; } = new();

        /// <summary>
        /// 请求耗时（毫秒）
        /// </summary>
        public long ElapsedMilliseconds { get; set; }
    }

    /// <summary>
    /// 医院API配置
    /// </summary>
    public class HospitalApiConfig
    {
        /// <summary>
        /// 医院编码
        /// </summary>
        public string HospitalCode { get; set; } = string.Empty;

        /// <summary>
        /// 服务器地址
        /// </summary>
        public string ServerAddress { get; set; } = string.Empty;

        /// <summary>
        /// 端口号
        /// </summary>
        public int? Port { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        public string? Username { get; set; }

        /// <summary>
        /// 密码
        /// </summary>
        public string? Password { get; set; }

        /// <summary>
        /// 其他参数
        /// </summary>
        public string? OtherParameters { get; set; }

        /// <summary>
        /// 基础URL
        /// </summary>
        public string BaseUrl => Port.HasValue 
            ? $"{ServerAddress}:{Port.Value}" 
            : ServerAddress;

        /// <summary>
        /// 认证令牌
        /// </summary>
        public string? AuthToken { get; set; }

        /// <summary>
        /// 时区ID
        /// </summary>
        public string? TimezoneId { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// 自定义请求头
        /// </summary>
        public Dictionary<string, string> CustomHeaders { get; set; } = new();
    }
}