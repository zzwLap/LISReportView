using Microsoft.AspNetCore.Authentication.Cookies;
using LisReportServer.Services;
using LisReportServer.Data;
using LisReportServer.Middleware;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages(options =>
{
    // 全局授权策略：除了首页、登录、登出、隐私页面外，都需要身份验证
    options.Conventions.AuthorizeFolder("/"); // 对所有页面启用授权
    options.Conventions.AllowAnonymousToPage("/Index"); // 首页不需要登录
    options.Conventions.AllowAnonymousToPage("/Login"); // 登录页不需要登录
    options.Conventions.AllowAnonymousToPage("/Logout"); // 登出页不需要登录
    options.Conventions.AllowAnonymousToPage("/Privacy"); // 隐私页不需要登录
});





// 添加HttpContextAccessor服务
builder.Services.AddHttpContextAccessor();

// 添加基础时区服务
builder.Services.AddScoped<ITimezoneService, BasicTimezoneService>();

// 添加Cookie服务
builder.Services.AddScoped<ICookieService, CookieService>();

// 添加报告服务
builder.Services.AddSingleton<IReportService, ReportService>();

// 添加医院服务器配置服务
builder.Services.AddScoped<IHospitalServerConfigService, HospitalServerConfigService>();

// 添加健康检查服务
builder.Services.AddScoped<IHealthCheckService, HealthCheckService>();

// 添加健康状态发布后台服务
builder.Services.AddHostedService<HealthStatusPublishingService>();

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

// 添加身份验证服务
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

var app = builder.Build();

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
