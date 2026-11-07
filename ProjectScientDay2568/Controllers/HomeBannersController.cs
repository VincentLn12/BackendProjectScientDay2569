using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ProjectScientDay2568.Data;
using ProjectScientDay2568.Helper;
using ProjectScientDay2568.Models;

namespace ProjectScientDay2568.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HomeBannersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        public HomeBannersController(ApplicationDbContext context, IWebHostEnvironment env) { _context = context; _env = env; }

        [AllowAnonymous, HttpGet]
        public async Task<IActionResult> Get() => Ok(new ApiResponse { Result = await _context.HomeBanners.OrderBy(x => x.SortOrder).ToListAsync() });

        [Authorize(Roles = "Admin,admin"), HttpPost, Consumes("multipart/form-data")]
        [EnableRateLimiting(RateLimitPolicies.Upload)]
        public async Task<IActionResult> Post([FromForm] HomeBannerForm form)
        {
            if (form.Image == null || !FileHelper.IsAllowedExt(form.Image.FileName, ".jpg", ".jpeg", ".png", ".webp")) return BadRequest("กรุณาอัปโหลดไฟล์รูปภาพ JPG, PNG หรือ WEBP");
            var banner = new HomeBanner { ImagePath = await FileHelper.SaveFileAsync(form.Image, _env.WebRootPath, new[] { "home-banners" }, "banner"), SortOrder = form.SortOrder, IsActive = form.IsActive, ShowInPopup = form.ShowInPopup };
            _context.HomeBanners.Add(banner); await _context.SaveChangesAsync(); return Ok(new ApiResponse { Result = banner });
        }

        [Authorize(Roles = "Admin,admin"), HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] HomeBannerUpdate form)
        {
            var banner = await _context.HomeBanners.FindAsync(id); if (banner == null) return NotFound();
            banner.SortOrder = form.SortOrder; banner.IsActive = form.IsActive; banner.ShowInPopup = form.ShowInPopup; await _context.SaveChangesAsync(); return Ok(new ApiResponse { Result = banner });
        }

        [Authorize(Roles = "Admin,admin"), HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var banner = await _context.HomeBanners.FindAsync(id); if (banner == null) return NotFound();
            FileHelper.DeleteFile(_env.WebRootPath, banner.ImagePath); _context.HomeBanners.Remove(banner); await _context.SaveChangesAsync(); return NoContent();
        }
    }
    public class HomeBannerForm { public IFormFile? Image { get; set; } public int SortOrder { get; set; } public bool IsActive { get; set; } = true; public bool ShowInPopup { get; set; } }
    public class HomeBannerUpdate { public int SortOrder { get; set; } public bool IsActive { get; set; } public bool ShowInPopup { get; set; } }
}
