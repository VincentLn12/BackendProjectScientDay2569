using ProjectScientDay2568.Helper;
using ProjectScientDay2568.Models.Dtos;

namespace ProjectScientDay2568.Service
{
    public class NewsService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IWebHostEnvironment _env;

        public NewsService(ApplicationDbContext context, IMapper mapper, IWebHostEnvironment env)
        {
            _context = context;
            _mapper = mapper;
            _env = env;
        }
        public async Task<List<NewsDto>> GetAllNewsAsync()
        {
            var News = await _context.News.OrderByDescending(p => p.DatePublished).ToListAsync();
            return _mapper.Map<List<NewsDto>>(News);
        }
        public async Task<NewsDto?> GetNewsByIdAsync(int id)
        {
            var news = await _context.News.FindAsync(id);
            if (news == null)
            {
                throw new KeyNotFoundException($"news with ID {id} not found.");
            }
            return _mapper.Map<NewsDto>(news);
        }

        public async Task<NewsDto> CreateNewsAsync(NewsDto dto, IFormFile? cover, List<IFormFile>? content)
        {
            var entity = _mapper.Map<News>(dto);
            await _context.News.AddAsync(entity);
            await _context.SaveChangesAsync();

            if (cover != null)
            {
                entity.CoverImagePath = await FileHelper.SaveFileAsync(
                    cover, _env.WebRootPath, new[] { "news", entity.Id.ToString() }, "cover");
            }

            if (content?.Any() == true)
            {
                entity.ContentImagePaths = await FileHelper.SaveFilesAsync(
                    content, _env.WebRootPath, new[] { "news", entity.Id.ToString() }, "img");
            }

            await _context.SaveChangesAsync();
            return _mapper.Map<NewsDto>(entity);
        }


        public async Task<NewsDto> UpdateNewsAsync(
         int id,
         NewsDto newsDto,
         IFormFile? newCoverImage = null,
         List<IFormFile>? newContentImages = null,
         List<string>? removeContentImagePaths = null,
         bool replaceContentImages = false)
        {
            if (newsDto == null)
                throw new ArgumentNullException(nameof(newsDto), "news cannot be null.");

            var entity = await _context.News.FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException($"news with ID {id} not found.");

            // --- Guard / Prepare ---
            var keepCover = entity.CoverImagePath;
            var keepContents = entity.ContentImagePaths?.ToList() ?? new List<string>();

            _mapper.Map(newsDto, entity);
            entity.CoverImagePath = keepCover;
            entity.ContentImagePaths = keepContents;

            var newlySavedFiles = new List<string>();
            string NormalizeRel(string rel) => rel?.Replace("\\", "/").TrimStart('/') ?? "";

            // ตั้งกติกาไฟล์ (ปรับได้)
            const long MAX_BYTES = 5 * 1024 * 1024; // 5MB
            bool Allowed(IFormFile f) =>
                FileHelper.IsAllowedExt(f.FileName, ".jpg", ".jpeg", ".png", ".webp", ".gif") &&
                FileHelper.IsAllowedSize(f.Length, MAX_BYTES);

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                // ----- Remove some content images -----
                if (removeContentImagePaths?.Any() == true && entity.ContentImagePaths?.Any() == true)
                {
                    var removeSet = new HashSet<string>(
                        removeContentImagePaths.Select(NormalizeRel),
                        StringComparer.OrdinalIgnoreCase);

                    var existingNormalized = entity.ContentImagePaths
                        .Select(p => (Raw: p, Norm: NormalizeRel(p)))
                        .ToList();

                    var toRemove = existingNormalized
                        .Where(x => removeSet.Contains(x.Norm))
                        .ToList();

                    foreach (var item in toRemove)
                    {
                        FileHelper.DeleteFile(_env.WebRootPath, item.Norm);
                        entity.ContentImagePaths.Remove(item.Raw);
                    }
                }

                // ----- Replace all content images (optional) -----
                if (replaceContentImages && entity.ContentImagePaths?.Any() == true)
                {
                    foreach (var rel in entity.ContentImagePaths.Select(NormalizeRel).ToList())
                        FileHelper.DeleteFile(_env.WebRootPath, rel);

                    entity.ContentImagePaths.Clear();
                }

                // ----- Upload new cover (if any) -----
                if (newCoverImage != null)
                {
                    if (!Allowed(newCoverImage))
                        throw new InvalidOperationException("รูปหน้าปกไม่ผ่านเกณฑ์ชนิดไฟล์หรือขนาด");

                    // ลบของเดิมก่อน (ถ้ามี)
                    if (!string.IsNullOrWhiteSpace(entity.CoverImagePath))
                        FileHelper.DeleteFile(_env.WebRootPath, NormalizeRel(entity.CoverImagePath));

                    var coverRel = await FileHelper.SaveFileAsync(
                        newCoverImage, _env.WebRootPath, new[] { "news", entity.Id.ToString() }, "cover");

                    entity.CoverImagePath = coverRel.Replace("\\", "/");
                    newlySavedFiles.Add(entity.CoverImagePath);
                }

                // ----- Append new content images (if any) -----
                if (newContentImages?.Any() == true)
                {
                    if (entity.ContentImagePaths == null) entity.ContentImagePaths = new List<string>();

                    foreach (var file in newContentImages)
                    {
                        if (file == null || file.Length == 0) continue;
                        if (!Allowed(file))
                            throw new InvalidOperationException("ไฟล์รูปเนื้อหาไม่ผ่านเกณฑ์ชนิดไฟล์หรือขนาด");

                        var rel = await FileHelper.SaveFileAsync(
                            file, _env.WebRootPath, new[] { "news", entity.Id.ToString() }, "img");

                        var norm = NormalizeRel(rel);
                        // กันซ้ำ
                        if (!entity.ContentImagePaths.Any(p =>
                                string.Equals(NormalizeRel(p), norm, StringComparison.OrdinalIgnoreCase)))
                        {
                            entity.ContentImagePaths.Add(norm);
                            newlySavedFiles.Add(norm);
                        }
                    }
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return _mapper.Map<NewsDto>(entity);
            }
            catch
            {
                // Rollback physical files ที่เพิ่งอัปโหลด
                foreach (var rel in newlySavedFiles.Select(NormalizeRel))
                    FileHelper.DeleteFile(_env.WebRootPath, rel);

                await tx.RollbackAsync();
                throw;
            }
        }
        public async Task DeleteNewsAsync(int id)
        {
            var news = await _context.News.FindAsync(id);

            if (news == null) throw new KeyNotFoundException("news notfound");

            _context.News.Remove(news);
            await _context.SaveChangesAsync();
        }


    }
}
