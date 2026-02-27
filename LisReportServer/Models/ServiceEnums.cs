namespace LisReportServer.Models
{
    /// <summary>
    /// 服务类别枚举
    /// </summary>
    public static class ServiceCategory
    {
        public const string LIS = "LIS系统";
        public const string HIS = "HIS系统";
        public const string PACS = "PACS系统";
        public const string RIS = "RIS系统";
        public const string EMR = "EMR系统";
        public const string Laboratory = "检验系统";
        public const string Imaging = "影像系统";
        public const string Pharmacy = "药房系统";
        public const string Billing = "收费系统";
        public const string Other = "其他";

        /// <summary>
        /// 获取所有服务类别
        /// </summary>
        public static List<string> GetAll()
        {
            return new List<string>
            {
                LIS,
                HIS,
                PACS,
                RIS,
                EMR,
                Laboratory,
                Imaging,
                Pharmacy,
                Billing,
                Other
            };
        }

        /// <summary>
        /// 获取服务类别的显示名称映射
        /// </summary>
        public static Dictionary<string, string> GetDisplayNames()
        {
            return new Dictionary<string, string>
            {
                { LIS, "实验室信息系统" },
                { HIS, "医院信息系统" },
                { PACS, "影像归档和通信系统" },
                { RIS, "放射科信息系统" },
                { EMR, "电子病历系统" },
                { Laboratory, "检验系统" },
                { Imaging, "影像系统" },
                { Pharmacy, "药房系统" },
                { Billing, "收费系统" },
                { Other, "其他系统" }
            };
        }
    }

    /// <summary>
    /// 认证类型枚举
    /// </summary>
    public static class AuthType
    {
        public const string None = "None";
        public const string Basic = "Basic";
        public const string OAuth2 = "OAuth2";
        public const string ApiKey = "ApiKey";
        public const string Bearer = "Bearer";

        public static List<string> GetAll()
        {
            return new List<string>
            {
                None,
                Basic,
                OAuth2,
                ApiKey,
                Bearer
            };
        }
    }
}
