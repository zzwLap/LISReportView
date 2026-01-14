namespace LisReportServer.Services
{
    public interface ITokenBlacklistService
    {
        Task<bool> IsTokenBlacklistedAsync(string tokenId);
        Task AddTokenToBlacklistAsync(string tokenId, DateTime expirationTime);
        Task RemoveExpiredTokensAsync();
    }
}