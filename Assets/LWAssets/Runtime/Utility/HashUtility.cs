using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace LWAssets
{
    /// <summary>
    /// 哈希工具类
    /// </summary>
    public static class HashUtility
    {
        /// <summary>
        /// 计算字符串的MD5
        /// </summary>
        public static string ComputeMD5(string input)
        {
            using (var md5 = MD5.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(input);
                var hash = md5.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }
        
        /// <summary>
        /// 计算字节数组的MD5
        /// </summary>
        public static string ComputeMD5(byte[] data)
        {
            using (var md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(data);
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }
        
        /// <summary>
        /// 计算文件的MD5
        /// </summary>
        public static string ComputeFileMD5(string filePath)
        {
            if (!File.Exists(filePath)) return null;
            
            using (var stream = File.OpenRead(filePath))
            using (var md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }
        
        /// <summary>
        /// 计算文件的CRC32
        /// </summary>
        public static uint ComputeFileCRC32(string filePath)
        {
            if (!File.Exists(filePath)) return 0;
            
            var data = File.ReadAllBytes(filePath);
            return ComputeCRC32(data);
        }
        
        /// <summary>
        /// 计算CRC32
        /// </summary>
        public static uint ComputeCRC32(byte[] data)
        {
            uint crc = 0xFFFFFFFF;
            
            foreach (byte b in data)
            {
                crc ^= b;
                for (int i = 0; i < 8; i++)
                {
                    crc = (crc >> 1) ^ (0xEDB88320 * (crc & 1));
                }
            }
            
            return crc ^ 0xFFFFFFFF;
        }
    }
}
