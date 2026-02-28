using System.Security.Cryptography;
using System.Text;

namespace LisReportServer.Helpers
{
    /// <summary>
    /// 加密解密辅助类
    /// </summary>
    public static class CryptoHelper
    {
        // 用于前后端通信的加密密钥（应该从配置文件读取）
        private const string DefaultKey = "LisReportServer2026SecretKey!!";
        
        /// <summary>
        /// AES加密
        /// </summary>
        /// <param name="plainText">明文</param>
        /// <param name="key">密钥（可选，默认使用配置的密钥）</param>
        /// <returns>Base64编码的密文</returns>
        public static string Encrypt(string plainText, string? key = null)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            var encryptKey = key ?? DefaultKey;
            var keyBytes = GetValidKeyBytes(encryptKey);

            using var aes = Aes.Create();
            aes.Key = keyBytes;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            // 将IV和加密数据组合在一起
            var result = new byte[aes.IV.Length + encryptedBytes.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);

            return Convert.ToBase64String(result);
        }

        /// <summary>
        /// AES解密
        /// </summary>
        /// <param name="cipherText">Base64编码的密文</param>
        /// <param name="key">密钥（可选，默认使用配置的密钥）</param>
        /// <returns>明文</returns>
        public static string Decrypt(string cipherText, string? key = null)
        {
            if (string.IsNullOrEmpty(cipherText))
                return string.Empty;

            var encryptKey = key ?? DefaultKey;
            var keyBytes = GetValidKeyBytes(encryptKey);
            var cipherBytes = Convert.FromBase64String(cipherText);

            using var aes = Aes.Create();
            aes.Key = keyBytes;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            // 提取IV
            var iv = new byte[aes.IV.Length];
            var encryptedData = new byte[cipherBytes.Length - iv.Length];
            Buffer.BlockCopy(cipherBytes, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(cipherBytes, iv.Length, encryptedData, 0, encryptedData.Length);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            var decryptedBytes = decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);

            return Encoding.UTF8.GetString(decryptedBytes);
        }

        /// <summary>
        /// 获取有效的密钥字节（确保长度为16、24或32字节）
        /// </summary>
        private static byte[] GetValidKeyBytes(string key)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            
            // AES支持128、192、256位密钥（16、24、32字节）
            int validLength = keyBytes.Length switch
            {
                <= 16 => 16,
                <= 24 => 24,
                _ => 32
            };

            var validKeyBytes = new byte[validLength];
            
            if (keyBytes.Length >= validLength)
            {
                Buffer.BlockCopy(keyBytes, 0, validKeyBytes, 0, validLength);
            }
            else
            {
                // 如果密钥太短，用原密钥填充
                Buffer.BlockCopy(keyBytes, 0, validKeyBytes, 0, keyBytes.Length);
                for (int i = keyBytes.Length; i < validLength; i++)
                {
                    validKeyBytes[i] = keyBytes[i % keyBytes.Length];
                }
            }

            return validKeyBytes;
        }

        /// <summary>
        /// RSA生成密钥对
        /// </summary>
        /// <returns>(公钥, 私钥)</returns>
        public static (string publicKey, string privateKey) GenerateRsaKeyPair()
        {
            using var rsa = RSA.Create(2048);
            var publicKey = Convert.ToBase64String(rsa.ExportRSAPublicKey());
            var privateKey = Convert.ToBase64String(rsa.ExportRSAPrivateKey());
            return (publicKey, privateKey);
        }

        /// <summary>
        /// RSA公钥加密
        /// </summary>
        public static string RsaEncrypt(string plainText, string publicKeyBase64)
        {
            using var rsa = RSA.Create();
            rsa.ImportRSAPublicKey(Convert.FromBase64String(publicKeyBase64), out _);
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var encryptedBytes = rsa.Encrypt(plainBytes, RSAEncryptionPadding.OaepSHA256);
            return Convert.ToBase64String(encryptedBytes);
        }

        /// <summary>
        /// RSA私钥解密
        /// </summary>
        public static string RsaDecrypt(string cipherText, string privateKeyBase64)
        {
            using var rsa = RSA.Create();
            rsa.ImportRSAPrivateKey(Convert.FromBase64String(privateKeyBase64), out _);
            var cipherBytes = Convert.FromBase64String(cipherText);
            var decryptedBytes = rsa.Decrypt(cipherBytes, RSAEncryptionPadding.OaepSHA256);
            return Encoding.UTF8.GetString(decryptedBytes);
        }
    }
}
