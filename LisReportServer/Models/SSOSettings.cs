namespace LisReportServer.Models
{
    public class SSOSettings
    {
        public bool Enabled { get; set; } = false;
        public string Authority { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string RedirectUri { get; set; } = string.Empty;
        public string ResponseType { get; set; } = "code";
        public string Scope { get; set; } = "openid profile email";
    }
}