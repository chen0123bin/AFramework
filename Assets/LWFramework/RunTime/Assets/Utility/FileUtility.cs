using System;
using System.IO;
using System.Threading.Tasks;

namespace LWAssets
{
    /// <summary>
    /// 文件工具类
    /// </summary>
    public static class FileUtility
    {
        /// <summary>
        /// 确保目录存在
        /// </summary>
        public static void EnsureDirectory(string path)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
        
        /// <summary>
        /// 安全删除文件
        /// </summary>
        public static bool SafeDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// 安全移动文件
        /// </summary>
        public static bool SafeMove(string source, string destination)
        {
            try
            {
                EnsureDirectory(destination);
                SafeDelete(destination);
                File.Move(source, destination);
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// 获取文件大小
        /// </summary>
        public static long GetFileSize(string path)
        {
            if (!File.Exists(path)) return 0;
            return new FileInfo(path).Length;
        }
        
        /// <summary>
        /// 格式化文件大小
        /// </summary>
        public static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;
            
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            
            return $"{size:0.##} {sizes[order]}";
        }
        
        /// <summary>
        /// 复制目录
        /// </summary>
        public static void CopyDirectory(string source, string destination, bool overwrite = true)
        {
            if (!Directory.Exists(source)) return;
            
            if (!Directory.Exists(destination))
            {
                Directory.CreateDirectory(destination);
            }
            
            foreach (var file in Directory.GetFiles(source))
            {
                var destFile = Path.Combine(destination, Path.GetFileName(file));
                File.Copy(file, destFile, overwrite);
            }
            
            foreach (var dir in Directory.GetDirectories(source))
            {
                var destDir = Path.Combine(destination, Path.GetFileName(dir));
                CopyDirectory(dir, destDir, overwrite);
            }
        }
    }
}
