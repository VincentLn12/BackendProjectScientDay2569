using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectScientDay2568.Data;
using ProjectScientDay2568.Helper;
using ProjectScientDay2568.Models;

namespace ProjectScientDay2568.Controllers
{
    // คู่มือการใช้งานสำหรับผู้ใช้ทั่วไป (ครู/นักเรียน) เก็บใน wwwroot/uploads/settings เหมือนคู่มือแอดมิน
    // ข้อมูลไฟล์อยู่ในตาราง SystemSettings (อ่านได้จาก GET /api/SystemSettings)
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,admin")]
    public class UserManualController : ControllerBase
    {
        private const long MaxBytes = 20 * 1024 * 1024;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public UserManualController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpPost, Consumes("multipart/form-data")]
        [EnableRateLimiting(RateLimitPolicies.Upload)]
        [RequestSizeLimit(MaxBytes + 1024 * 1024)]
        public async Task<IActionResult> Upload(IFormFile? file)
        {
            if (file == null || !FileHelper.IsAllowedExt(file.FileName, ".pdf"))
                return BadRequest("กรุณาอัปโหลดไฟล์ PDF");
            if (!FileHelper.IsAllowedSize(file.Length, MaxBytes))
                return BadRequest("ไฟล์ต้องมีขนาดไม่เกิน 20MB");

            // ตรวจหัวไฟล์ว่าเป็น PDF จริง ไม่ใช่แค่เปลี่ยนนามสกุล
            var header = new byte[5];
            await using (var s = file.OpenReadStream())
            {
                if (await s.ReadAsync(header) < 5 || System.Text.Encoding.ASCII.GetString(header) != "%PDF-")
                    return BadRequest("ไฟล์ไม่ใช่ PDF ที่ถูกต้อง");
            }

            var setting = await _context.SystemSettings.FirstOrDefaultAsync();
            if (setting == null)
            {
                setting = new SystemSetting();
                _context.SystemSettings.Add(setting);
            }

            var oldPath = setting.UserManualPath;
            setting.UserManualPath = await FileHelper.SaveFileAsync(file, _env.WebRootPath, new[] { "settings" }, "user-manual");
            setting.UserManualFileName = Path.GetFileName(file.FileName);
            setting.UserManualUpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            // ลบไฟล์เก่าหลังบันทึกฐานข้อมูลสำเร็จแล้วเท่านั้น
            if (!string.IsNullOrEmpty(oldPath)) FileHelper.DeleteFile(_env.WebRootPath, oldPath);

            return Ok(new ApiResponse { Result = setting });
        }

        [HttpDelete]
        public async Task<IActionResult> Delete()
        {
            var setting = await _context.SystemSettings.FirstOrDefaultAsync();
            if (setting == null || string.IsNullOrEmpty(setting.UserManualPath)) return NoContent();

            var path = setting.UserManualPath;
            setting.UserManualPath = null;
            setting.UserManualFileName = null;
            setting.UserManualUpdatedAt = null;
            await _context.SaveChangesAsync();

            FileHelper.DeleteFile(_env.WebRootPath, path);
            return NoContent();
        }
    }
}
