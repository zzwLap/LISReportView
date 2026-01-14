using LisReportServer.Data;
using LisReportServer.Models;
using Microsoft.EntityFrameworkCore;

namespace LisReportServer.Services
{
    public class HospitalServerConfigService : IHospitalServerConfigService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public HospitalServerConfigService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<List<HospitalServerConfig>> GetAllConfigsAsync()
        {
            return await _context.HospitalServerConfigs
                .OrderByDescending(h => h.CreatedAt)
                .ToListAsync();
        }

        public async Task<HospitalServerConfig?> GetByIdAsync(int id)
        {
            return await _context.HospitalServerConfigs.FindAsync(id);
        }

        public async Task<HospitalServerConfig> CreateAsync(HospitalServerConfig config)
        {
            // 检查医院编码是否已存在
            var existingConfig = await _context.HospitalServerConfigs
                .FirstOrDefaultAsync(h => h.HospitalCode == config.HospitalCode);
            
            if (existingConfig != null)
            {
                throw new ArgumentException($"医院编码 '{config.HospitalCode}' 已存在，请使用不同的编码。");
            }
            
            // 加密密码
            if (!string.IsNullOrEmpty(config.EncryptedPassword))
            {
                config.EncryptedPassword = EncryptPassword(config.EncryptedPassword);
            }
            
            config.CreatedAt = DateTime.UtcNow;
            config.UpdatedAt = DateTime.UtcNow;
            
            _context.HospitalServerConfigs.Add(config);
            await _context.SaveChangesAsync();
            return config;
        }

        public async Task<HospitalServerConfig> UpdateAsync(HospitalServerConfig config)
        {
            var existingConfig = await _context.HospitalServerConfigs.FindAsync(config.Id);
            if (existingConfig == null)
            {
                throw new ArgumentException($"Hospital server config with ID {config.Id} not found.");
            }

            // 检查医院编码是否与其他配置冲突（排除当前配置）
            var conflictingConfig = await _context.HospitalServerConfigs
                .FirstOrDefaultAsync(h => h.HospitalCode == config.HospitalCode && h.Id != config.Id);
            
            if (conflictingConfig != null)
            {
                throw new ArgumentException($"医院编码 '{config.HospitalCode}' 已被其他配置使用，请使用不同的编码。");
            }

            // 更新非密码字段
            existingConfig.HospitalName = config.HospitalName;
            existingConfig.HospitalCode = config.HospitalCode;
            existingConfig.ServerAddress = config.ServerAddress;
            existingConfig.Port = config.Port;
            existingConfig.Username = config.Username;
            existingConfig.OtherParameters = config.OtherParameters;
            existingConfig.IsActive = config.IsActive;

            // 如果提供了新密码，则加密并更新
            if (!string.IsNullOrEmpty(config.EncryptedPassword))
            {
                existingConfig.EncryptedPassword = EncryptPassword(config.EncryptedPassword);
            }

            existingConfig.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            return existingConfig;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var config = await _context.HospitalServerConfigs.FindAsync(id);
            if (config == null)
            {
                return false;
            }

            _context.HospitalServerConfigs.Remove(config);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleStatusAsync(int id)
        {
            var config = await _context.HospitalServerConfigs.FindAsync(id);
            if (config == null)
            {
                return false;
            }

            config.IsActive = !config.IsActive;
            config.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<HospitalServerConfig>> SearchConfigsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return await GetAllConfigsAsync();
            }

            var normalizedSearchTerm = searchTerm.ToLowerInvariant();
            
            return await _context.HospitalServerConfigs
                .Where(h => h.HospitalName.ToLower().Contains(normalizedSearchTerm) ||
                           h.HospitalCode.ToLower().Contains(normalizedSearchTerm) ||
                           h.ServerAddress.ToLower().Contains(normalizedSearchTerm) ||
                           (h.Username != null && h.Username.ToLower().Contains(normalizedSearchTerm)))
                .OrderByDescending(h => h.CreatedAt)
                .ToListAsync();
        }

        private string EncryptPassword(string password)
        {
            // 简单的加密实现（在生产环境中应使用更强的加密方法）
            // 这里使用配置中的密钥进行简单的加密
            var encryptionKey = _configuration.GetValue<string>("EncryptionKey", "LisReportServerDefaultKey");
            
            // 确保密钥长度为AES所需的长度（16, 24, 或 32 字节）
            var keyBytes = GetValidKeyBytes(encryptionKey);
            
            using var aes = System.Security.Cryptography.Aes.Create();
            aes.Key = keyBytes;
            aes.GenerateIV();
            
            var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            
            var passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);
            var encryptedBytes = encryptor.TransformFinalBlock(passwordBytes, 0, passwordBytes.Length);
            
            // 将IV和加密后的数据合并
            var result = new byte[aes.IV.Length + encryptedBytes.Length];
            Array.Copy(aes.IV, 0, result, 0, aes.IV.Length);
            Array.Copy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);
            
            return Convert.ToBase64String(result);
        }

        private byte[] GetValidKeyBytes(string key)
        {
            // 将密钥转换为UTF8字节数组
            var keyBytes = System.Text.Encoding.UTF8.GetBytes(key);
            
            // AES支持的密钥长度：16, 24, 32 字节 (对应 128, 192, 256 位)
            // 我们选择32字节（256位）作为默认长度
            const int desiredKeyLength = 32;
            
            if (keyBytes.Length == desiredKeyLength)
            {
                return keyBytes;
            }
            
            var result = new byte[desiredKeyLength];
            if (keyBytes.Length > desiredKeyLength)
            {
                // 如果原密钥太长，截取前面的部分
                Array.Copy(keyBytes, result, desiredKeyLength);
            }
            else
            {
                // 如果原密钥太短，填充零字节
                Array.Copy(keyBytes, result, keyBytes.Length);
                // 其余位置自动为0
            }
            
            return result;
        }

        public string DecryptPassword(string encryptedPassword)
        {
            if (string.IsNullOrEmpty(encryptedPassword))
                return string.Empty;

            try
            {
                var encryptionKey = _configuration.GetValue<string>("EncryptionKey", "LisReportServerDefaultKey");
                
                // 使用相同的密钥处理逻辑
                var keyBytes = GetValidKeyBytes(encryptionKey);

                var fullBytes = Convert.FromBase64String(encryptedPassword);
                
                // 提取IV（前16字节）和加密数据
                var iv = new byte[16];
                var encryptedData = new byte[fullBytes.Length - 16];
                
                Array.Copy(fullBytes, 0, iv, 0, 16);
                Array.Copy(fullBytes, 16, encryptedData, 0, encryptedData.Length);

                using var aes = System.Security.Cryptography.Aes.Create();
                aes.Key = keyBytes;
                aes.IV = iv;

                var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                var decryptedBytes = decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);

                return System.Text.Encoding.UTF8.GetString(decryptedBytes);
            }
            catch
            {
                // 如果解密失败，返回原始值（这在某些情况下可能是合理的降级行为）
                return encryptedPassword;
            }
        }
    }
}