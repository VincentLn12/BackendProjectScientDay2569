using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectScientDay2568.Data;
using ProjectScientDay2568.Helper;
using ProjectScientDay2568.Models;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace ProjectScientDay2568.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TrainingtopicsController : ControllerBase
    {
        private const string StaffRoles = "Admin,admin,Staff,staff,เจ้าหน้าที่";
        private const long MaxQrBytes = 5 * 1024 * 1024;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public TrainingtopicsController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        private IActionResult HandleResponse(ApiResponse response)
        {
            return StatusCode((int)response.StatusCode, response);
        }

        private bool IsStaff() => StaffRoles.Split(',').Any(User.IsInRole);

        // กลุ่มไลน์: เจ้าหน้าที่เห็นทั้งหมด คนทั่วไปเห็นแค่ว่ามีกลุ่มหรือไม่ (ผู้สมัครรับลิงก์จาก TrainingRegistrations/my)
        private void PrepareLineGroup(Trainingtopic t)
        {
            if (IsStaff())
                t.HasLineGroup = !string.IsNullOrEmpty(t.LineGroupUrl) || !string.IsNullOrEmpty(t.LineQrPath);
            else
                t.HideLineGroup();
        }

        private static string? CleanText(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        private static string? CleanLineUrl(string? s)
        {
            var url = CleanText(s);
            return url != null && Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                   (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
                ? url
                : null;
        }

        // อัปโหลด/เปลี่ยนรูป QR กลุ่มไลน์ของหลักสูตร
        [Authorize(Roles = StaffRoles)]
        [HttpPost("{id}/line-qr"), Consumes("multipart/form-data")]
        [RequestSizeLimit(MaxQrBytes + 1024 * 1024)]
        public async Task<IActionResult> UploadLineQr(int id, IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("กรุณาเลือกรูป QR");
            if (!FileHelper.IsAllowedExt(file.FileName, ".png", ".jpg", ".jpeg", ".webp"))
                return BadRequest("รูป QR ต้องเป็นไฟล์ PNG, JPG หรือ WEBP");
            if (!FileHelper.IsAllowedSize(file.Length, MaxQrBytes))
                return BadRequest("รูปต้องมีขนาดไม่เกิน 5MB");

            var topic = await _context.Trainingtopics.FindAsync(id);
            if (topic == null) return NotFound("ไม่พบหลักสูตร");

            var oldPath = topic.LineQrPath;
            topic.LineQrPath = await FileHelper.SaveFileAsync(file, _env.WebRootPath, new[] { "training", "line-qr" }, "line-qr");
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(oldPath)) FileHelper.DeleteFile(_env.WebRootPath, oldPath);
            return Ok(new ApiResponse { IsSuccess = true, StatusCode = HttpStatusCode.OK, Result = topic.LineQrPath });
        }

        [Authorize(Roles = StaffRoles)]
        [HttpDelete("{id}/line-qr")]
        public async Task<IActionResult> DeleteLineQr(int id)
        {
            var topic = await _context.Trainingtopics.FindAsync(id);
            if (topic == null || string.IsNullOrEmpty(topic.LineQrPath)) return NoContent();

            var path = topic.LineQrPath;
            topic.LineQrPath = null;
            await _context.SaveChangesAsync();

            FileHelper.DeleteFile(_env.WebRootPath, path);
            return NoContent();
        }

        // เลยวันปิดรับสมัครแล้ว → ปิดรับสมัคร (IsOpenRegister = false) ให้อัตโนมัติ
        // เรียกก่อนอ่านข้อมูล หน้าแอดมินและหน้าผู้ใช้จึงเห็นสถานะ "ปิดรับสมัคร" ตรงกับความจริง
        private Task<int> CloseExpiredRegistrationsAsync()
        {
            var now = DateTime.UtcNow;
            return _context.Trainingtopics
                .Where(t => t.IsOpenRegister && t.RegisterEndDate != null && t.RegisterEndDate < now)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsOpenRegister, false));
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] int? yearId)
        {
            var response = new ApiResponse();
            try
            {
                await CloseExpiredRegistrationsAsync();
                var query = _context.Trainingtopics.AsNoTracking();
                if (yearId.HasValue)
                    query = query.Where(t => t.YearID == yearId.Value);

                var topics = await query
                    .Include(t => t.branch)
                    .Include(t => t.CertificateTemplate)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();

                foreach (var t in topics)
                {
                    PrepareLineGroup(t);
                    t.RegisteredCount = await _context.TrainingRegistrations
                        .CountAsync(r => r.TrainingtopicId == t.Id && r.Status != "ยกเลิกแล้ว");
                }

                response.StatusCode = HttpStatusCode.OK;
                response.IsSuccess = true;
                response.Result = topics;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }
            return HandleResponse(response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var response = new ApiResponse();
            try
            {
                await CloseExpiredRegistrationsAsync();
                var topic = await _context.Trainingtopics.AsNoTracking()
                    .Include(t => t.branch)
                    .Include(t => t.CertificateTemplate)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (topic == null)
                {
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.IsSuccess = false;
                    response.ErrorMessages.Add("ไม่พบข้อมูลหัวข้ออบรม");
                    return NotFound(response);
                }

                PrepareLineGroup(topic);
                topic.RegisteredCount = await _context.TrainingRegistrations
                    .CountAsync(r => r.TrainingtopicId == topic.Id && r.Status != "ยกเลิกแล้ว");

                response.StatusCode = HttpStatusCode.OK;
                response.IsSuccess = true;
                response.Result = topic;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }
            return HandleResponse(response);
        }

        private static string NormalizeTimeString(string? timeStr)
        {
            if (string.IsNullOrWhiteSpace(timeStr)) return string.Empty;
            return timeStr.Replace('.', ':').Trim();
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] TrainingtopicDto model)
        {
            var response = new ApiResponse();
            try
            {
                if (!ModelState.IsValid)
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.IsSuccess = false;
                    response.ErrorMessages.AddRange(ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));
                    return BadRequest(response);
                }

                if (string.IsNullOrWhiteSpace(model.Name))
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.IsSuccess = false;
                    response.ErrorMessages.Add("กรุณาระบุชื่อหลักสูตรอบรม");
                    return BadRequest(response);
                }

                if (CleanText(model.LineGroupUrl) != null && CleanLineUrl(model.LineGroupUrl) == null)
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.IsSuccess = false;
                    response.ErrorMessages.Add("ลิงก์กลุ่มไลน์ต้องขึ้นต้นด้วย https://");
                    return BadRequest(response);
                }

                TimeSpan? start = null;
                string cleanStart = NormalizeTimeString(model.StartTime);
                if (TimeSpan.TryParse(cleanStart, out var sVal)) start = sVal;

                TimeSpan? end = null;
                string cleanEnd = NormalizeTimeString(model.EndTime);
                if (TimeSpan.TryParse(cleanEnd, out var eVal)) end = eVal;

                var topic = new Trainingtopic
                {
                    Name = model.Name,
                    Description = model.Description,
                    TrainingDate = model.TrainingDate,
                    StartTime = start,
                    EndTime = end,
                    MaxParticipants = model.MaxParticipants,
                    IsOpenRegister = model.IsOpenRegister,
                    RegisterStartDate = model.RegisterStartDate,
                    RegisterEndDate = model.RegisterEndDate,
                    Location = model.Location,
                    TeacherName = model.TeacherName,
                    Trainingtopicsrequirements = model.Trainingtopicsrequirements,
                    Branch_id = model.Branch_id,
                    CertificateTemplateId = model.CertificateTemplateId,
                    YearID = model.YearID,
                    Audience = model.Audience,
                    LineGroupUrl = CleanLineUrl(model.LineGroupUrl),
                    LineNote = CleanText(model.LineNote)
                };

                _context.Trainingtopics.Add(topic);
                await _context.SaveChangesAsync();

                response.StatusCode = HttpStatusCode.Created;
                response.IsSuccess = true;
                response.Result = topic;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.InnerException?.Message ?? ex.Message);
            }
            return HandleResponse(response);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] TrainingtopicDto model)
        {
            var response = new ApiResponse();
            try
            {
                if (!ModelState.IsValid)
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.IsSuccess = false;
                    response.ErrorMessages.AddRange(ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));
                    return BadRequest(response);
                }

                if (string.IsNullOrWhiteSpace(model.Name))
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.IsSuccess = false;
                    response.ErrorMessages.Add("กรุณาระบุชื่อหลักสูตรอบรม");
                    return BadRequest(response);
                }

                var topic = await _context.Trainingtopics.FindAsync(id);
                if (topic == null)
                {
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.IsSuccess = false;
                    response.ErrorMessages.Add("ไม่พบหัวข้ออบรมที่ต้องการแก้ไข");
                    return NotFound(response);
                }

                if (CleanText(model.LineGroupUrl) != null && CleanLineUrl(model.LineGroupUrl) == null)
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.IsSuccess = false;
                    response.ErrorMessages.Add("ลิงก์กลุ่มไลน์ต้องขึ้นต้นด้วย https://");
                    return BadRequest(response);
                }

                TimeSpan? start = null;
                string cleanStart = NormalizeTimeString(model.StartTime);
                if (TimeSpan.TryParse(cleanStart, out var sVal)) start = sVal;

                TimeSpan? end = null;
                string cleanEnd = NormalizeTimeString(model.EndTime);
                if (TimeSpan.TryParse(cleanEnd, out var eVal)) end = eVal;

                topic.Name = model.Name;
                topic.Description = model.Description;
                topic.TrainingDate = model.TrainingDate;
                topic.StartTime = start;
                topic.EndTime = end;
                topic.MaxParticipants = model.MaxParticipants;
                topic.IsOpenRegister = model.IsOpenRegister;
                topic.RegisterStartDate = model.RegisterStartDate;
                topic.RegisterEndDate = model.RegisterEndDate;
                topic.Location = model.Location;
                topic.TeacherName = model.TeacherName;
                topic.Trainingtopicsrequirements = model.Trainingtopicsrequirements;
                topic.Branch_id = model.Branch_id;
                topic.CertificateTemplateId = model.CertificateTemplateId;
                topic.YearID = model.YearID;
                topic.Audience = model.Audience;
                topic.LineGroupUrl = CleanLineUrl(model.LineGroupUrl);
                topic.LineNote = CleanText(model.LineNote);

                _context.Trainingtopics.Update(topic);
                await _context.SaveChangesAsync();

                response.StatusCode = HttpStatusCode.OK;
                response.IsSuccess = true;
                response.Result = topic;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.InnerException?.Message ?? ex.Message);
            }
            return HandleResponse(response);
        }

        // คัดลอกหลักสูตรทั้งปีไปอีกปี: เลื่อนวันที่ตามจำนวนปีที่ต่างกัน, ปิดรับสมัครไว้ก่อน, ไม่คัดลอกผู้สมัคร
        // ข้ามหลักสูตรที่ชื่อซ้ำกับที่มีอยู่แล้วในปีปลายทาง (กดซ้ำได้ไม่เกิดรายการซ้ำ)
        [Authorize(Roles = "Admin,admin,Staff,staff,เจ้าหน้าที่")]
        [HttpPost("copy-year")]
        public async Task<IActionResult> CopyYear([FromBody] CopyYearDto model)
        {
            var response = new ApiResponse();
            if (model.FromYearId == model.ToYearId)
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                response.IsSuccess = false;
                response.ErrorMessages.Add("ปีต้นทางและปีปลายทางต้องไม่ใช่ปีเดียวกัน");
                return BadRequest(response);
            }

            var fromYear = await _context.Years.FindAsync(model.FromYearId);
            var toYear = await _context.Years.FindAsync(model.ToYearId);
            if (fromYear == null || toYear == null)
            {
                response.StatusCode = HttpStatusCode.NotFound;
                response.IsSuccess = false;
                response.ErrorMessages.Add("ไม่พบปีการจัดงาน");
                return NotFound(response);
            }

            var sources = await _context.Trainingtopics.AsNoTracking().Where(t => t.YearID == model.FromYearId).ToListAsync();
            var existingNames = (await _context.Trainingtopics.Where(t => t.YearID == model.ToYearId).Select(t => t.Name).ToListAsync())
                .Select(n => n.Trim()).ToHashSet();

            var shift = toYear.YearNumber - fromYear.YearNumber;
            DateTime? Move(DateTime? d) => d?.AddYears(shift);

            var copies = sources
                .Where(t => !existingNames.Contains(t.Name.Trim()))
                .Select(t => new Trainingtopic
                {
                    Name = t.Name,
                    Description = t.Description,
                    TrainingDate = Move(t.TrainingDate),
                    StartTime = t.StartTime,
                    EndTime = t.EndTime,
                    MaxParticipants = t.MaxParticipants,
                    IsOpenRegister = false,
                    RegisterStartDate = Move(t.RegisterStartDate),
                    RegisterEndDate = Move(t.RegisterEndDate),
                    Location = t.Location,
                    IsActive = t.IsActive,
                    TeacherName = t.TeacherName,
                    Trainingtopicsrequirements = t.Trainingtopicsrequirements,
                    Branch_id = t.Branch_id,
                    CertificateTemplateId = t.CertificateTemplateId,
                    Audience = t.Audience,
                    YearID = model.ToYearId,
                })
                .ToList();

            _context.Trainingtopics.AddRange(copies);
            await _context.SaveChangesAsync();

            response.StatusCode = HttpStatusCode.OK;
            response.IsSuccess = true;
            response.Result = new { copied = copies.Count, skipped = sources.Count - copies.Count };
            return Ok(response);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var response = new ApiResponse();
            try
            {
                var topic = await _context.Trainingtopics.FindAsync(id);
                if (topic == null)
                {
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.IsSuccess = false;
                    response.ErrorMessages.Add("ไม่พบข้อมูลหลักสูตรที่ต้องการลบ");
                    return NotFound(response);
                }

                var qrPath = topic.LineQrPath;
                _context.Trainingtopics.Remove(topic);
                await _context.SaveChangesAsync();
                if (!string.IsNullOrEmpty(qrPath)) FileHelper.DeleteFile(_env.WebRootPath, qrPath);

                response.StatusCode = HttpStatusCode.OK;
                response.IsSuccess = true;
                response.Result = "ลบข้อมูลสำเร็จ";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }
            return HandleResponse(response);
        }
    }

    public class CopyYearDto
    {
        public int FromYearId { get; set; }
        public int ToYearId { get; set; }
    }

    public class TrainingtopicDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public DateTime? TrainingDate { get; set; }
        public string? StartTime { get; set; }
        public string? EndTime { get; set; }
        public int MaxParticipants { get; set; }
        public bool IsOpenRegister { get; set; }
        public DateTime? RegisterStartDate { get; set; }
        public DateTime? RegisterEndDate { get; set; }
        public string? Location { get; set; }
        public string? TeacherName { get; set; }
        public string? Trainingtopicsrequirements { get; set; }
        public int? Branch_id { get; set; }
        public int? CertificateTemplateId { get; set; }
        public int? YearID { get; set; }
        public string? Audience { get; set; }
        public string? LineGroupUrl { get; set; }
        public string? LineNote { get; set; }
    }
}
