using Microsoft.EntityFrameworkCore;
using ProjectScientDay2568.Models.Dtos.Team;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using static ProjectScientDay2568.Utility.SD;
using SixLabors.ImageSharp.Processing;

namespace ProjectScientDay2568.Service
{
    public class CertificateConfigService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IMapper _mapper;

        public CertificateConfigService(ApplicationDbContext context, IWebHostEnvironment env , IMapper mapper)
        {
            _context = context;
            _env = env;
            _mapper = mapper;
        }
        public async Task<CertificateConfigDto?> GetCertificateConfigAsync(int yearId, CancellationToken ct = default)
        {
            var cfg = await _context.CertificateConfigs
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.YearID == yearId, ct);

            return cfg == null ? null : _mapper.Map<CertificateConfigDto>(cfg);
        }
        public async Task<CertificateConfigDto> CreateCertificateConfigAsync(CertificateConfigDto dto, CancellationToken ct = default)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            var yearExists = await _context.Years.AnyAsync(y => y.YearID == dto.YearID, ct);
            if (!yearExists)
                throw new KeyNotFoundException($"Year {dto.YearID} not found.");

            var exists = await _context.CertificateConfigs.AnyAsync(x => x.YearID == dto.YearID, ct);
            if (exists)
                throw new InvalidOperationException($"มีค่าตั้งค่าสำหรับปี {dto.YearID} อยู่แล้ว");

            dto.CertificateName = (dto.CertificateName ?? string.Empty).Trim();

            var entity = _mapper.Map<CertificateConfig>(dto);
            entity.Year = null; // กัน AutoMapper ผูก navigation โดยไม่จำเป็น

            _context.Attach(new Year { YearID = dto.YearID }).State = EntityState.Unchanged;

            await _context.CertificateConfigs.AddAsync(entity, ct);
            await _context.SaveChangesAsync(ct);

            return _mapper.Map<CertificateConfigDto>(entity);
        }
        public async Task<CertificateConfigDto> UpdateCertificateConfigAsync(
            int id, UpdateCertificateConfigRequest dto, CancellationToken ct = default)
        {
            var entity = await _context.CertificateConfigs
                .FirstOrDefaultAsync(c => c.YearID == id, ct)
                ?? throw new KeyNotFoundException($"CertificateConfig (YearID={id}) ไม่พบ");

            entity.StartNumber = dto.StartNumber;
            entity.CertificateName = dto.CertificateName?.Trim() ?? "";
            entity.UniversityDeanTitle = dto.UniversityDeanTitle?.Trim();
            entity.UniversityDeanName = dto.UniversityDeanName?.Trim();
            entity.FacultyDeanTitle = dto.FacultyDeanTitle?.Trim();
            entity.FacultyDeanName = dto.FacultyDeanName?.Trim();

            await _context.SaveChangesAsync(ct);
            return _mapper.Map<CertificateConfigDto>(entity);
        }
        public async Task<string?> UploadCertificateBackgroundAsync(
            int yearId, IFormFile file, CancellationToken ct = default)
        {
            // 1) ตรวจปี
            var yearExists = await _context.Years
                .AsNoTracking()
                .AnyAsync(y => y.YearID == yearId, ct);
            if (!yearExists)
                throw new KeyNotFoundException($"Year {yearId} not found");

            // 2) ตรวจไฟล์
            if (file == null || file.Length == 0)
                throw new InvalidOperationException("No file uploaded");

            var allowed = new[] { ".png", ".jpg", ".jpeg" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
                throw new InvalidOperationException("Only .png .jpg .jpeg are allowed.");

            // 3) โหลด/สร้าง CertificateConfig (<< แก้ new ชนิดให้ถูกต้อง)
            var cfg = await _context.CertificateConfigs
                .FirstOrDefaultAsync(c => c.YearID == yearId, ct);

            if (cfg == null)
            {
                cfg = new CertificateConfig { YearID = yearId };
                _context.CertificateConfigs.Add(cfg);
            }

            // 4) เตรียม path
            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var dir = Path.Combine(webRoot, "uploads", "years", yearId.ToString());
            Directory.CreateDirectory(dir);

            var fileName = "certificate-bg" + ext;
            var physicalPath = Path.Combine(dir, fileName);

            // 5) ลบไฟล์เก่า (ถ้าชื่อ/สกุลเปลี่ยน)
            if (!string.IsNullOrWhiteSpace(cfg.CertificateBackgroundPath))
            {
                var oldPhysical = Path.Combine(
                    webRoot,
                    cfg.CertificateBackgroundPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

                if (!string.Equals(oldPhysical, physicalPath, StringComparison.OrdinalIgnoreCase)
                    && File.Exists(oldPhysical))
                {
                    try { File.Delete(oldPhysical); } catch { /* ignore */ }
                }
            }

            // 6) บันทึกไฟล์ใหม่
            using (var fs = new FileStream(physicalPath, FileMode.Create, FileAccess.Write, FileShare.None))
                await file.CopyToAsync(fs, ct);

            // 7) อัปเดต web path ใน CertificateConfig
            var webPath = $"/uploads/years/{yearId}/{fileName}";
            cfg.CertificateBackgroundPath = webPath;

            await _context.SaveChangesAsync(ct);
            return webPath;
        }
        public async Task<byte[]?> GetCertificateBackgroundBytesAsync(
            int yearId, CancellationToken ct = default)
        {
            var webPath = await _context.CertificateConfigs
                .AsNoTracking()
                .Where(c => c.YearID == yearId)
                .Select(c => c.CertificateBackgroundPath)
                .FirstOrDefaultAsync(ct);

            if (string.IsNullOrWhiteSpace(webPath)) return null;

            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var physical = Path.Combine(webRoot, webPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(physical)) return null;

            return await File.ReadAllBytesAsync(physical, ct);
        }
        public async Task<string?> GetCertificateBackgroundWebPathAsync(
            int yearId, CancellationToken ct = default)
        {
            return await _context.CertificateConfigs
                .AsNoTracking()
                .Where(c => c.YearID == yearId)
                .Select(c => c.CertificateBackgroundPath)
                .FirstOrDefaultAsync(ct);
        }
        public async Task<string?> UploadSignatureAsync(
        int yearId, IFormFile file, SignatureRole role, CancellationToken ct = default)
        {
            // ตรวจ year
            var yearExists = await _context.Years
                .AsNoTracking()
                .AnyAsync(y => y.YearID == yearId, ct);
            if (!yearExists)
                throw new KeyNotFoundException($"Year {yearId} not found");

            if (file == null || file.Length == 0)
                throw new InvalidOperationException("No file uploaded");

            var allowed = new[] { ".png", ".jpg", ".jpeg" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
                throw new InvalidOperationException("Only .png .jpg .jpeg are allowed.");

            // โหลด/สร้าง CertificateConfig
            var cfg = await _context.CertificateConfigs
                .FirstOrDefaultAsync(c => c.YearID == yearId, ct);
            if (cfg == null)
            {
                cfg = new CertificateConfig { YearID = yearId };
                _context.CertificateConfigs.Add(cfg);
                await _context.SaveChangesAsync(ct);
            }

            // เตรียมโฟลเดอร์
            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var dir = Path.Combine(webRoot, "uploads", "years", yearId.ToString(), "signatures");
            Directory.CreateDirectory(dir);

            // ตั้งชื่อไฟล์ตาม role -> บันทึกเป็น .png เสมอ
            var baseName = role == SignatureRole.UniversityDean
                ? "university-dean-signature"
                : "faculty-dean-signature";

            var fileName = baseName + ".png"; // เราจงใจเขียนเป็น PNG เสมอ
            var physicalPath = Path.Combine(dir, fileName);

            // หา path เดิมเพื่อลบ (ถ้ามีและไม่ใช่ไฟล์เดียวกัน)
            string? oldWebPath = role == SignatureRole.UniversityDean
                ? cfg.UniversityDeanSignature
                : cfg.FacultyDeanSignature;

            if (!string.IsNullOrWhiteSpace(oldWebPath))
            {
                var oldPhysical = Path.Combine(
                    webRoot,
                    oldWebPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

                if (!string.Equals(oldPhysical, physicalPath, StringComparison.OrdinalIgnoreCase)
                    && File.Exists(oldPhysical))
                {
                    try { File.Delete(oldPhysical); } catch { /* ignore */ }
                }
            }

            // อ่านไฟล์อัปโหลดเข้า memory
            byte[] inputBytes;
            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms, ct);
                inputBytes = ms.ToArray();
            }

            // === พารามิเตอร์มาตรฐานของลายเซ็น ===
            // ขนาด "แคนวาส" สุดท้ายที่อยากให้เท่ากันทุกไฟล์ (ปรับได้)
            const int CANVAS_WIDTH = 600;   // ความกว้างมาตรฐาน (px)
            const int CANVAS_HEIGHT = 180;  // ความสูงมาตรฐาน (px)

            // ขนาดลายเซ็นสูงสุดภายในแคนวาส (เผื่อ margin เล็กน้อย)
            const int MAX_SIGNATURE_WIDTH = 520;
            const int MAX_SIGNATURE_HEIGHT = 120;

            // แปลงพื้นหลังขาวให้โปร่งใส + ครอป content
            var cleaned = await MakeSignatureTransparentAsync(inputBytes, whiteThreshold: 240);

            using (var sig = Image.Load<Rgba32>(cleaned ?? inputBytes))
            {
                // คำนวณสเกลให้พอดีกรอบสูงสุด (คงสัดส่วน)
                var scaleW = (double)MAX_SIGNATURE_WIDTH / sig.Width;
                var scaleH = (double)MAX_SIGNATURE_HEIGHT / sig.Height;
                var scale = Math.Min(1.0, Math.Min(scaleW, scaleH)); // ไม่ขยายเกิน 100% เพื่อคงความคม (ถ้าต้องการให้ขยายได้ เอา Math.Min ออก)

                var targetW = (int)Math.Round(sig.Width * scale);
                var targetH = (int)Math.Round(sig.Height * scale);

                // รีไซซ์ลายเซ็น
                sig.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(targetW, targetH),
                    Mode = ResizeMode.Max, // คงสัดส่วน
                    Sampler = KnownResamplers.Lanczos3
                }));

                // สร้างแคนวาสโปร่งใสขนาดคงที่
                using var canvas = new Image<Rgba32>(CANVAS_WIDTH, CANVAS_HEIGHT, new Rgba32(0, 0, 0, 0));

                // วางลายเซ็นกลางแคนวาส
                var offsetX = (CANVAS_WIDTH - sig.Width) / 2;
                var offsetY = (CANVAS_HEIGHT - sig.Height) / 2;
                canvas.Mutate(x => x.DrawImage(sig, new Point(offsetX, offsetY), 1f));

                // บันทึกเป็น PNG โปร่งใสเสมอ
                await using var fsOut = new FileStream(physicalPath, FileMode.Create, FileAccess.Write, FileShare.None);
                await canvas.SaveAsPngAsync(fsOut, ct);
            }

            // อัปเดต web path
            var webPath = $"/uploads/years/{yearId}/signatures/{fileName}";
            if (role == SignatureRole.UniversityDean)
                cfg.UniversityDeanSignature = webPath;
            else
                cfg.FacultyDeanSignature = webPath;

            await _context.SaveChangesAsync(ct);
            return webPath;
        }
        static async Task<byte[]?> MakeSignatureTransparentAsync(byte[]? input, byte whiteThreshold = 240, byte alphaOut = 0)
        {
            if (input == null || input.Length == 0) return null;

            using var img = Image.Load<Rgba32>(input);

            // 1) ลบพื้นหลังขาว/เกือบขาวให้โปร่งใส
            img.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    var row = accessor.GetRowSpan(y);
                    for (int x = 0; x < row.Length; x++)
                    {
                        ref Rgba32 p = ref row[x];
                        if (p.R >= whiteThreshold && p.G >= whiteThreshold && p.B >= whiteThreshold)
                            p.A = alphaOut;
                    }
                }
            });

            // 2) ครอปให้พอดีคอนเทนต์
            var cropRect = CropToContent(img);
            if (!cropRect.IsEmpty)
                img.Mutate(op => op.Crop(cropRect));

            using var ms = new MemoryStream();
            await img.SaveAsPngAsync(ms);
            return ms.ToArray();
        }
        static Rectangle CropToContent(Image<Rgba32> img)
        {
            int minX = img.Width, minY = img.Height, maxX = -1, maxY = -1;

            img.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    var row = accessor.GetRowSpan(y);
                    for (int x = 0; x < row.Length; x++)
                    {
                        var p = row[x];
                        if (p.A > 5) // มีความทึบ
                        {
                            if (x < minX) minX = x;
                            if (y < minY) minY = y;
                            if (x > maxX) maxX = x;
                            if (y > maxY) maxY = y;
                        }
                    }
                }
            });

            if (maxX < minX || maxY < minY) return Rectangle.Empty;

            int pad = 2;
            minX = Math.Max(0, minX - pad);
            minY = Math.Max(0, minY - pad);
            maxX = Math.Min(img.Width - 1, maxX + pad);
            maxY = Math.Min(img.Height - 1, maxY + pad);

            return new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }
        public async Task<string?> GetSignatureWebPathAsync(
        int yearId, SignatureRole role, CancellationToken ct = default)
        {
            return await _context.CertificateConfigs
                .AsNoTracking()
                .Where(c => c.YearID == yearId)
                .Select(c => role == SignatureRole.UniversityDean
                    ? c.UniversityDeanSignature
                    : c.FacultyDeanSignature)
                .FirstOrDefaultAsync(ct);
        }
        public async Task<byte[]?> GetSignatureBytesAsync(
        int yearId, SignatureRole role, CancellationToken ct = default)
        {
            var webPath = await GetSignatureWebPathAsync(yearId, role, ct);
            if (string.IsNullOrWhiteSpace(webPath)) return null;

            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var physical = Path.Combine(webRoot, webPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(physical)) return null;

            return await File.ReadAllBytesAsync(physical, ct);
        }
        public async Task<string?> UploadLogoAsync(
         int yearId, IFormFile file, LogoRole role, CancellationToken ct = default)
        {
            // ตรวจ year
            var yearExists = await _context.Years
                .AsNoTracking()
                .AnyAsync(y => y.YearID == yearId, ct);
            if (!yearExists)
                throw new KeyNotFoundException($"Year {yearId} not found");

            if (file == null || file.Length == 0)
                throw new InvalidOperationException("No file uploaded");

            var allowed = new[] { ".png", ".jpg", ".jpeg" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
                throw new InvalidOperationException("Only .png .jpg .jpeg are allowed.");

            // โหลด/สร้าง CertificateConfig
            var cfg = await _context.CertificateConfigs
                .FirstOrDefaultAsync(c => c.YearID == yearId, ct);
            if (cfg == null)
            {
                cfg = new CertificateConfig { YearID = yearId };
                _context.CertificateConfigs.Add(cfg);
                await _context.SaveChangesAsync(ct);
            }

            // เตรียมโฟลเดอร์
            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var dir = Path.Combine(webRoot, "uploads", "years", yearId.ToString(), "Logo");
            Directory.CreateDirectory(dir);

            // ตั้งชื่อไฟล์ตาม role
            var baseName = role == LogoRole.UniversityLogo
                ? "university-Logo"
                : "institute-Logo";

            var fileName = baseName + ext;
            var physicalPath = Path.Combine(dir, fileName);

            // หา path เดิมเพื่อลบ (ถ้ามีและไม่ใช่ไฟล์เดียวกัน)
            string? oldWebPath = role == LogoRole.UniversityLogo
                ? cfg.UniversityLogoPath
                : cfg.InstituteLogoPath;

            if (!string.IsNullOrWhiteSpace(oldWebPath))
            {
                var oldPhysical = Path.Combine(
                    webRoot,
                    oldWebPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

                if (!string.Equals(oldPhysical, physicalPath, StringComparison.OrdinalIgnoreCase)
                    && File.Exists(oldPhysical))
                {
                    try { File.Delete(oldPhysical); } catch { /* ignore */ }
                }
            }

            // เขียนไฟล์ใหม่
            using (var fs = new FileStream(physicalPath, FileMode.Create, FileAccess.Write, FileShare.None))
                await file.CopyToAsync(fs, ct);

            // อัปเดต web path
            var webPath = $"/uploads/years/{yearId}/logo/{fileName}";
            if (role == LogoRole.UniversityLogo)
                cfg.UniversityLogoPath = webPath;
            else
                cfg.InstituteLogoPath = webPath;

            await _context.SaveChangesAsync(ct);
            return webPath;
        }
    }
}
