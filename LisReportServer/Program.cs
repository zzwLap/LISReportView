using Microsoft.AspNetCore.Authentication.Cookies;
using LisReportServer.Services;

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

// 添加Cookie服务
builder.Services.AddScoped<ICookieService, CookieService>();

// 添加报告服务
builder.Services.AddSingleton<IReportService, ReportService>();

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
});

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

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

// 映射API控制器
app.MapControllers();



app.Run();
