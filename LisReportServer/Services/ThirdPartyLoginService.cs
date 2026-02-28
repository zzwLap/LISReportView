using LisReportServer.Models;
using System.Text;
using System.Text.Json;

namespace LisReportServer.Services
{
    /// <summary>
    /// 第三方登录服务实现
    /// </summary>
    public class ThirdPartyLoginService : IThirdPartyLoginService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHospitalServiceConfigService _serviceConfigService;
        private readonly IHospitalProfileService _profileService;
        private readonly ILisTokenCacheService _tokenCacheService;
        private readonly ILogger<ThirdPartyLoginService> _logger;

        public ThirdPartyLoginService(
            IHttpClientFactory httpClientFactory,
            IHospitalServiceConfigService serviceConfigService,
            IHospitalProfileService profileService,
            ILisTokenCacheService tokenCacheService,
            ILogger<ThirdPartyLoginService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _serviceConfigService = serviceConfigService;
            _profileService = profileService;
            _tokenCacheService = tokenCacheService;
            _logger = logger;
        }

        public async Task<ThirdPartyLoginResult> AuthenticateAsync(string hospitalName, string username, string password)
        {
            try
            {
                // 1. 根据医院名称获取医院配置
                var hospital = await _profileService.GetByNameAsync(hospitalName);
                if (hospital == null)
                {
                    _logger.LogWarning("医院配置不存在: {HospitalName}", hospitalName);
                    return new ThirdPartyLoginResult
                    {
                        Success = false,
                        ErrorMessage = $"医院 '{hospitalName}' 的配置不存在"
                    };
                }

                // 2. 获取该医院的LIS服务配置（根据ServiceCategory分类）
                var lisServiceConfig = await _serviceConfigService.GetByCategoryAsync(ServiceCategory.LIS)
                    .ContinueWith(t => t.Result.FirstOrDefault(s => s.HospitalProfileId == hospital.Id && s.IsActive));
                
                if (lisServiceConfig == null)
                {
                    _logger.LogWarning("医院 {HospitalName} 未配置LIS服务或服务未启用", hospitalName);
                    return new ThirdPartyLoginResult
                    {
                        Success = false,
                        ErrorMessage = $"医院 '{hospitalName}' 未配置LIS系统服务"
                    };
                }

                // 3. 构建请求URL
                string apiUrl = BuildApiUrl(lisServiceConfig, "/api/auth/LoginSystem");
                _logger.LogInformation("调用LIS登录API: {Url}", apiUrl);

                // 4. 构建请求体
                var requestBody = new ThirdPartyLoginRequest
                {
                    UserName = username,
                    Password = password
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // 5. 创建HTTP客户端
                var httpClient = _httpClientFactory.CreateClient();
                
                // 设置超时时间
                if (lisServiceConfig.Timeout.HasValue)
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(lisServiceConfig.Timeout.Value);
                }

                // 添加认证头（如果配置了）
                if (!string.IsNullOrEmpty(lisServiceConfig.ApiKey))
                {
                    httpClient.DefaultRequestHeaders.Add("X-API-Key", lisServiceConfig.ApiKey);
                }
                else if (!string.IsNullOrEmpty(lisServiceConfig.Username) && !string.IsNullOrEmpty(lisServiceConfig.EncryptedPassword))
                {
                    var basicAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{lisServiceConfig.Username}:{lisServiceConfig.EncryptedPassword}"));
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {basicAuth}");
                }

                // 6. 发送请求（支持重试）
                HttpResponseMessage? response = null;
                int retryCount = lisServiceConfig.RetryCount ?? 0;
                
                for (int i = 0; i <= retryCount; i++)
                {
                    try
                    {
                        response = await httpClient.PostAsync(apiUrl, httpContent);
                        if (response.IsSuccessStatusCode)
                        {
                            break;
                        }
                        
                        if (i < retryCount)
                        {
                            _logger.LogWarning("第三方登录API调用失败，准备重试 ({Current}/{Total})", i + 1, retryCount);
                            await Task.Delay(1000 * (i + 1)); // 递增延迟
                        }
                    }
                    catch (Exception ex) when (i < retryCount)
                    {
                        _logger.LogWarning(ex, "第三方登录API调用异常，准备重试 ({Current}/{Total})", i + 1, retryCount);
                        await Task.Delay(1000 * (i + 1));
                    }
                }

                // 7. 处理响应
                if (response == null || !response.IsSuccessStatusCode)
                {
                    var statusCode = response?.StatusCode.ToString() ?? "Unknown";
                    var errorContent = response != null ? await response.Content.ReadAsStringAsync() : "No response";
                    _logger.LogError("第三方登录失败: StatusCode={StatusCode}, Response={Response}", statusCode, errorContent);
                    
                    return new ThirdPartyLoginResult
                    {
                        Success = false,
                        ErrorMessage = $"第三方登录失败，状态码: {statusCode}"
                    };
                }

                // 8. 解析响应
                var responseContent = await response.Content.ReadAsStringAsync();
                var loginResponse = JsonSerializer.Deserialize<ThirdPartyLoginResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (loginResponse == null || string.IsNullOrEmpty(loginResponse.AccessToken))
                {
                    _logger.LogError("LIS登录响应格式错误: {Response}", responseContent);
                    return new ThirdPartyLoginResult
                    {
                        Success = false,
                        ErrorMessage = "LIS登录响应格式错误"
                    };
                }

                // 9. 将Token存储到缓存中（默认30分钟过期）
                await _tokenCacheService.SetTokenAsync(
                    hospitalName, 
                    username, 
                    loginResponse.AccessToken, 
                    loginResponse.RefreshToken, 
                    expirationMinutes: 30);

                _logger.LogInformation("LIS用户 {Username} 通过医院 {HospitalName} 登录成功，Token已缓存", username, hospitalName);
                return new ThirdPartyLoginResult
                {
                    Success = true,
                    AccessToken = loginResponse.AccessToken,
                    RefreshToken = loginResponse.RefreshToken
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "第三方登录服务异常: Hospital={HospitalName}, User={Username}", hospitalName, username);
                return new ThirdPartyLoginResult
                {
                    Success = false,
                    ErrorMessage = $"登录服务异常: {ex.Message}"
                };
            }
        }

        public async Task ClearUserTokenAsync(string hospitalName, string username)
        {
            await _tokenCacheService.RemoveTokenAsync(hospitalName, username);
            _logger.LogInformation("已清除用户Token缓存: 医院={HospitalName}, 用户={Username}", hospitalName, username);
        }

        public async Task<LisTokenInfo?> GetUserTokenAsync(string hospitalName, string username)
        {
            return await _tokenCacheService.GetTokenAsync(hospitalName, username);
        }

        public async Task<bool> TestConnectionAsync(string hospitalName)
        {
            try
            {
                var hospital = await _profileService.GetByNameAsync(hospitalName);
                if (hospital == null)
                {
                    return false;
                }

                var lisServiceConfig = await _serviceConfigService.GetByCategoryAsync(ServiceCategory.LIS)
                    .ContinueWith(t => t.Result.FirstOrDefault(s => s.HospitalProfileId == hospital.Id && s.IsActive));
                
                if (lisServiceConfig == null)
                {
                    return false;
                }

                // 如果配置了健康检查URL，则测试健康检查
                if (!string.IsNullOrEmpty(lisServiceConfig.HealthCheckUrl))
                {
                    var httpClient = _httpClientFactory.CreateClient();
                    httpClient.Timeout = TimeSpan.FromSeconds(5);
                    
                    var response = await httpClient.GetAsync(lisServiceConfig.HealthCheckUrl);
                    return response.IsSuccessStatusCode;
                }

                // 否则尝试连接主服务地址
                string apiUrl = BuildApiUrl(lisServiceConfig, "/api/auth/LoginSystem");
                var testClient = _httpClientFactory.CreateClient();
                testClient.Timeout = TimeSpan.FromSeconds(5);
                
                var testResponse = await testClient.GetAsync(apiUrl);
                return testResponse.IsSuccessStatusCode || testResponse.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "测试第三方登录服务连接失败: {HospitalName}", hospitalName);
                return false;
            }
        }

        /// <summary>
        /// 构建API URL
        /// </summary>
        /// <param name="config">服务配置</param>
        /// <param name="apiPath">API路径，例如：/api/auth/LoginSystem</param>
        /// <returns>完整的API URL</returns>
        private string BuildApiUrl(HospitalServiceConfig config, string apiPath)
        {
            var baseUrl = config.ServiceAddress.TrimEnd('/');
            
            // 如果配置了端口，添加端口
            if (config.ServicePort.HasValue && !baseUrl.Contains($":{config.ServicePort}"))
            {
                var uri = new Uri(baseUrl);
                baseUrl = $"{uri.Scheme}://{uri.Host}:{config.ServicePort}{uri.PathAndQuery}";
            }

            // 添加网关前缀：/gateway/{ServiceDiscoveryKey}
            // ServiceDiscoveryKey 用于网关路由识别
            if (!string.IsNullOrEmpty(config.ServiceDiscoveryKey))
            {
                baseUrl = $"{baseUrl}/gateway/{config.ServiceDiscoveryKey}";
            }

            // 添加API版本（如果有）
            if (!string.IsNullOrEmpty(config.ApiVersion))
            {
                baseUrl = $"{baseUrl}/{config.ApiVersion}";
            }

            // 添加实际的API路径
            baseUrl = $"{baseUrl}{apiPath}";

            // 清理可能的双斜杠（但保留协议中的 ://）
            baseUrl = System.Text.RegularExpressions.Regex.Replace(baseUrl, "(?<!:)//+", "/");

            return baseUrl;
        }
    }
}
