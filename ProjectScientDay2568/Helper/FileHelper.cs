namespace ProjectScientDay2568.Helper
{
    public static class FileHelper
    {
        // ==== ของเดิม ====
        public static async Task<string> SaveFileAsync(
            IFormFile file,
            string webRootPath,
            string[] folders,
            string filePrefix = "file")
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("ไฟล์ว่าง");

            var ext = Path.GetExtension(file.FileName);
            var safeName = $"{filePrefix}_{Guid.NewGuid():N}{ext}";

            var relDir = Path.Combine(new[] { "uploads" }.Concat(folders).ToArray());
            var absDir = Path.Combine(webRootPath, relDir);
            Directory.CreateDirectory(absDir);

            // กัน path traversal
            EnsureUnderWebRoot(webRootPath, absDir);

            var absPath = Path.Combine(absDir, safeName);
            using (var stream = new FileStream(absPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Path.Combine(relDir, safeName).Replace("\\", "/");
        }

        public static async Task<List<string>> SaveFilesAsync(
            IEnumerable<IFormFile> files,
            string webRootPath,
            string[] folders,
            string filePrefix = "file")
        {
            var result = new List<string>();
            foreach (var (file, idx) in files.Select((f, i) => (f, i)))
            {
                var path = await SaveFileAsync(file, webRootPath, folders, $"{filePrefix}{idx:00}");
                result.Add(path);
            }
            return result;
        }

        public static void DeleteFile(string webRootPath, string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return;
            var absPath = Path.Combine(webRootPath, relativePath.TrimStart('/', '\\'));
            if (File.Exists(absPath))
            {
                File.Delete(absPath);
            }
        }

        // ==== ของใหม่เพิ่มเติม ====

        // ตรวจสอบนามสกุลไฟล์ที่อนุญาต
        public static bool IsAllowedExt(string fileName, params string[] allowedExts)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return false;
            var ext = (Path.GetExtension(fileName) ?? string.Empty).ToLowerInvariant();
            var set = allowedExts?.Length > 0
                ? new HashSet<string>(allowedExts.Select(e => e.ToLowerInvariant()))
                : new HashSet<string>(new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" });
            return set.Contains(ext);
        }

        // ตรวจสอบขนาดไฟล์ (ไบต์)
        public static bool IsAllowedSize(long length, long maxBytes) => length > 0 && length <= maxBytes;

        // ทำชื่อไฟล์ให้ปลอดภัย (a-z0-9-_)
        public static string SanitizeFileName(string fileNameWithoutExt)
        {
            var s = fileNameWithoutExt.Trim().ToLowerInvariant();
            foreach (var c in Path.GetInvalidFileNameChars())
                s = s.Replace(c, '-');
            s = System.Text.RegularExpressions.Regex.Replace(s, @"[^a-z0-9\-_]+", "-");
            s = System.Text.RegularExpressions.Regex.Replace(s, @"-+", "-").Trim('-');
            if (string.IsNullOrWhiteSpace(s)) s = "file";
            return s;
        }

        // เซฟหลายไฟล์แบบ atomic: ถ้าพลาดจะลบไฟล์ที่เซฟไปแล้ว
        public static async Task<List<string>> SaveFilesAtomicAsync(
            IEnumerable<IFormFile> files,
            string webRootPath,
            string[] folders,
            string filePrefix = "file")
        {
            var saved = new List<string>();
            try
            {
                foreach (var (file, idx) in files.Select((f, i) => (f, i)))
                {
                    var p = await SaveFileAsync(file, webRootPath, folders, $"{filePrefix}{idx:00}");
                    saved.Add(p);
                }
                return saved;
            }
            catch
            {
                DeleteFiles(webRootPath, saved);
                throw;
            }
        }

        // ลบหลายไฟล์
        public static void DeleteFiles(string webRootPath, IEnumerable<string> relativePaths)
        {
            foreach (var p in relativePaths ?? Enumerable.Empty<string>())
                DeleteFile(webRootPath, p);
        }

        // แปลงเป็น absolute path
        public static string GetAbsolutePath(string webRootPath, string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                throw new ArgumentException("relativePath ว่าง");
            var abs = Path.Combine(webRootPath, relativePath.TrimStart('/', '\\'));
            return abs;
        }

        // บังคับให้ path อยู่ใต้ wwwroot (กัน ../)
        public static void EnsureUnderWebRoot(string webRootPath, string absolutePathToCheck)
        {
            var root = Path.GetFullPath(webRootPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var target = Path.GetFullPath(absolutePathToCheck);
            if (!target.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Path อยู่นอก wwwroot");
        }

        // สร้าง URL สาธารณะ (เช่น https://host/uploads/..)
        public static string BuildPublicUrl(string baseUrl, string relativePath)
        {
            if (string.IsNullOrWhiteSpace(baseUrl)) throw new ArgumentException(nameof(baseUrl));
            var trimmedBase = baseUrl.TrimEnd('/');
            var rel = relativePath.TrimStart('/');
            return $"{trimmedBase}/{rel}";
        }

        // เซฟจาก byte[] (เช่น ไฟล์ที่มาจากการประมวลผล)
        public static async Task<string> SaveBytesAsync(
            byte[] bytes,
            string webRootPath,
            string[] folders,
            string extension,
            string filePrefix = "file")
        {
            if (bytes == null || bytes.Length == 0) throw new ArgumentException("ข้อมูลไฟล์ว่าง");
            if (!extension.StartsWith(".")) extension = "." + extension;

            var relDir = Path.Combine(new[] { "uploads" }.Concat(folders).ToArray());
            var absDir = Path.Combine(webRootPath, relDir);
            Directory.CreateDirectory(absDir);
            EnsureUnderWebRoot(webRootPath, absDir);

            var fileName = $"{filePrefix}_{Guid.NewGuid():N}{extension}";
            var absPath = Path.Combine(absDir, fileName);
            await File.WriteAllBytesAsync(absPath, bytes);

            return Path.Combine(relDir, fileName).Replace("\\", "/");
        }

        // เซฟจาก Base64 (เช่น data:image/png;base64,... หรือ base64 ล้วน)
        public static Task<string> SaveBase64Async(
            string base64,
            string webRootPath,
            string[] folders,
            string extensionIfMissing = ".bin",
            string filePrefix = "file")
        {
            if (string.IsNullOrWhiteSpace(base64)) throw new ArgumentException("base64 ว่าง");

            // รองรับรูปแบบ data URI
            var comma = base64.IndexOf(',');
            string payload = comma >= 0 ? base64[(comma + 1)..] : base64;

            var bytes = Convert.FromBase64String(payload);
            return SaveBytesAsync(bytes, webRootPath, folders, extensionIfMissing, filePrefix);
        }
    }
}
