using System.Security.Claims;

namespace LisReportServer.Helpers
{
    /// <summary>
    /// 权限验证辅助类
    /// </summary>
    public static class AuthorizationHelper
    {
        /// <summary>
        /// 检查用户是否为本地系统用户
        /// </summary>
        public static bool IsLocalSystemUser(ClaimsPrincipal user)
        {
            if (user?.Identity?.IsAuthenticated != true)
                return false;

            var hospitalNameClaim = user.FindFirst("HospitalName");
            return hospitalNameClaim?.Value == "系统";
        }

        /// <summary>
        /// 检查用户是否具有管理员角色
        /// </summary>
        public static bool IsAdminUser(ClaimsPrincipal user)
        {
            if (user?.Identity?.IsAuthenticated != true)
                return false;

            return user.IsInRole("Admin") || user.IsInRole("管理员");
        }

        /// <summary>
        /// 检查用户是否为本地管理员（同时满足本地系统用户和管理员角色）
        /// </summary>
        public static bool IsLocalAdmin(ClaimsPrincipal user)
        {
            return IsLocalSystemUser(user) && IsAdminUser(user);
        }

        /// <summary>
        /// 获取用户的医院名称
        /// </summary>
        public static string? GetUserHospitalName(ClaimsPrincipal user)
        {
            if (user?.Identity?.IsAuthenticated != true)
                return null;

            return user.FindFirst("HospitalName")?.Value;
        }

        /// <summary>
        /// 获取用户名
        /// </summary>
        public static string? GetUsername(ClaimsPrincipal user)
        {
            if (user?.Identity?.IsAuthenticated != true)
                return null;

            return user.FindFirst(ClaimTypes.Name)?.Value;
        }
    }
}
