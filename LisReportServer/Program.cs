using Microsoft.AspNetCore.Authentication.Cookies;
using LisReportServer.Services;
using LisReportServer.Data;
using LisReportServer.Middleware;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using LisReportServer.Models;
using ServiceMesh.Agent;

#pragma warning disable CS8604 // Possible null reference argument.

var builder = WebApplication.CreateBuilder(args);

// 添加配置
builder.Services.Configure<SSOSettings>(builder.Configuration.GetSection("SSOSettings"));

// Add services to the container.
builder.Services.AddHttpClient();
builder.Services.AddRazorPages(options =>
{
    // 全局授权策略：除了首页、登录、登出、隐私页面外，都需要身份验证
    options.Conventions.AuthorizeFolder("/"); // 对所有页面启用授权
    options.Conventions.AllowAnonymousToPage("/Index"); // 首页不需要登录
    options.Conventions.AllowAnonymousToPage("/Login"); // 登录页不需要登录
    options.Conventions.AllowAnonymousToPage("/Logout"); // 登出页不需要登录
    options.Conventions.AllowAnonymousToPage("/Privacy"); // 隐私页不需要登录
    options.Conventions.AllowAnonymousToPage("/Account/Login"); // SSO登录页不需要登录
    options.Conventions.AllowAnonymousToPage("/Account/Logout"); // SSO登出页不需要登录
});


// 添加HttpContextAccessor服务
builder.Services.AddHttpContextAccessor();

// 添加基础时区服务
builder.Services.AddScoped<ITimezoneService, BasicTimezoneService>();

// 添加用户认证服务
builder.Services.AddScoped<IUserAuthenticationService, UserAuthenticationService>();

// 添加SSO用户服务
builder.Services.AddScoped<ISSOUserService, SSOUserService>();

// 添加SSO健康检查服务
builder.Services.AddScoped<ISSOHealthCheckService, CachedSSOHealthCheckService>();

// 添加Cookie服务
builder.Services.AddScoped<ICookieService, CookieService>();

// 添加报告服务
builder.Services.AddSingleton<IReportService, ReportService>();

// 添加医院基本信息配置服务
builder.Services.AddScoped<IHospitalProfileService, HospitalProfileService>();

// 添加医院服务配置服务
builder.Services.AddScoped<IHospitalServiceConfigService, HospitalServiceConfigService>();

// 添加第三方登录服务
builder.Services.AddScoped<IThirdPartyLoginService, ThirdPartyLoginService>();

// 添加健康检查服务
builder.Services.AddScoped<IHealthCheckService, HealthCheckService>();

// 添加健康状态发布后台服务
builder.Services.AddHostedService<HealthStatusPublishingService>();

// 添加SSO状态监控后台服务
builder.Services.AddHostedService<SSOMonitoringService>();

// 添加Redis连接（如果配置了Redis连接字符串）
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrEmpty(redisConnectionString))
{
    builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString));
    builder.Services.AddScoped<ITokenBlacklistService, AdvancedTokenBlacklistService>();
}
else
{
    // 如果没有Redis配置，使用内存版本
    builder.Services.AddScoped<ITokenBlacklistService, TokenBlacklistService>();
}

// 添加后台服务清理过期的黑名单令牌
builder.Services.AddHostedService<TokenBlacklistCleanupService>();

// 添加内存缓存
builder.Services.AddMemoryCache();

// 添加数据库上下文
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// 添加数据库初始化服务
builder.Services.AddScoped<DatabaseInitializationService>();

// 根据配置决定使用哪种身份验证
var ssoSettings = builder.Configuration.GetSection("SSOSettings").Get<SSOSettings>();

if (ssoSettings?.Enabled == true)
{
    // 启用SSO
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = OpenIdConnectDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddOpenIdConnect(options =>
    {
        options.Authority = ssoSettings.Authority;
        options.ClientId = ssoSettings.ClientId;
        options.ClientSecret = ssoSettings.ClientSecret;
        options.ResponseType = ssoSettings.ResponseType;
        options.CallbackPath = "/signin-oidc";
        options.SignedOutRedirectUri = ssoSettings.RedirectUri.Replace("/signin-oidc", "");
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = "name",
            RoleClaimType = "role"
        };
        options.SaveTokens = true;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
    });
}
else
{
    // 使用本地身份验证
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.LogoutPath = "/Logout";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;

        // 通过工厂方法注入自定义事件
        options.EventsType = typeof(CustomCookieAuthenticationEvents);
    });

    // 注册自定义身份验证事件
    builder.Services.AddTransient<CustomCookieAuthenticationEvents>();
}

// 从配置读取服务信息
var serviceName = builder.Configuration.GetValue<string>("ServiceName");
var servicePort = builder.Configuration.GetValue<int>("Port");

// 添加服务自动注册
builder.Services.AddServiceRegistration(options =>
{
    options.ServiceName = serviceName ?? "";
    options.Port = servicePort;
    options.RegistryUrl = builder.Configuration.GetValue<string>("RegistryUrl") ?? "http://localhost:5000";
    options.Version = "1.0.0";
    options.HeartbeatInterval = TimeSpan.FromSeconds(30);
    options.EnableDefaultHealthCheck = true;
    options.Metadata = new Dictionary<string, string>
    {
        ["environment"] = "development",
        ["team"] = "platform"
    };
});

var app = builder.Build();

// 初始化数据库
using (var scope = app.Services.CreateScope())
{
    var initializationService = scope.ServiceProvider.GetRequiredService<DatabaseInitializationService>();
    await initializationService.InitializeAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

// 添加时区中间件
app.UseMiddleware<TimezoneMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

// 映射API控制器
app.MapControllers();

app.Run();