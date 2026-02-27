using LisReportServer.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LisReportServer.Filters
{
    /// <summary>
    /// 本地管理员授权过滤器
    /// 仅允许本地系统（医院名称为"系统"）的管理员角色访问
    /// </summary>
    public class LocalAdminAuthorizationFilter : IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            // 检查用户是否已认证
            if (user?.Identity?.IsAuthenticated != true)
            {
                context.Result = new RedirectToPageResult("/Login", new { returnUrl = context.HttpContext.Request.Path });
                return;
            }

            // 检查用户是否为本地管理员
            if (!AuthorizationHelper.IsLocalAdmin(user))
            {
                var hospitalName = AuthorizationHelper.GetUserHospitalName(user);
                var message = hospitalName == "系统" 
                    ? "您的账户没有管理员权限，无法访问此页面。" 
                    : $"此页面仅限本地系统管理员访问，您当前登录的医院为：{hospitalName}";
                
                context.Result = new RedirectToPageResult("/AccessDenied", new { message });
                return;
            }
        }
    }

    /// <summary>
    /// 本地管理员授权特性
    /// 用于标记需要本地管理员权限的页面或控制器
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class LocalAdminOnlyAttribute : TypeFilterAttribute
    {
        public LocalAdminOnlyAttribute() : base(typeof(LocalAdminAuthorizationFilter))
        {
        }
    }
}
