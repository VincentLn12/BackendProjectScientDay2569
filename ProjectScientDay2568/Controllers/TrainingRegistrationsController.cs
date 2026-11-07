using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectScientDay2568.Data;
using ProjectScientDay2568.Models;
using System;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ProjectScientDay2568.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TrainingRegistrationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TrainingRegistrationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private IActionResult HandleResponse(ApiResponse response)
        {
            return StatusCode((int)response.StatusCode, response);
        }

        private string? GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirst("sub")?.Value
                ?? User.FindFirst("id")?.Value;
        }

        // GET /api/TrainingRegistrations (สำหรับแอดมินดูรายชื่อทั้งหมด หรือกรองตามหลักสูตร)
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] int? trainingtopicId)
        {
            var response = new ApiResponse();
            try
            {
                var query = _context.TrainingRegistrations
                    .Include(r => r.Trainingtopic)
                    .Include(r => r.User)
                    .AsQueryable();

                if (trainingtopicId.HasValue)
                {
                    query = query.Where(r => r.TrainingtopicId == trainingtopicId.Value);
                }

                var list = await query.AsNoTracking().OrderByDescending(r => r.RegisteredAt).ToListAsync();
                // endpoint นี้ไม่ได้จำกัดสิทธิ์ จึงไม่ส่งลิงก์กลุ่มไลน์
                foreach (var r in list) r.Trainingtopic?.HideLineGroup();

                response.StatusCode = HttpStatusCode.OK;
                response.IsSuccess = true;
                response.Result = list;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }
            return HandleResponse(response);
        }

        // GET /api/TrainingRegistrations/my (ดึงเฉพาะของผู้ใช้ปัจจุบัน)
        [Authorize]
        [HttpGet("my")]
        public async Task<IActionResult> GetMy()
        {
            var response = new ApiResponse();
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    response.StatusCode = HttpStatusCode.Unauthorized;
                    response.IsSuccess = false;
                    response.ErrorMessages.Add("ไม่พบข้อมูลบัญชีผู้ใช้ หรือกรุณาเข้าสู่ระบบใหม่");
                    return Unauthorized(response);
                }

                var list = await _context.TrainingRegistrations
                    .Include(r => r.Trainingtopic)
                    .AsNoTracking()
                    .Where(r => r.UserId == userId)
                    .OrderByDescending(r => r.RegisteredAt)
                    .ToListAsync();

                // ลิงก์กลุ่มไลน์ให้เฉพาะรายการที่ยังไม่ยกเลิก
                foreach (var r in list)
                {
                    if (r.Trainingtopic == null) continue;
                    if (r.Status == "ยกเลิกแล้ว") r.Trainingtopic.HideLineGroup();
                    else r.Trainingtopic.HasLineGroup = !string.IsNullOrEmpty(r.Trainingtopic.LineGroupUrl) || !string.IsNullOrEmpty(r.Trainingtopic.LineQrPath);
                }

                response.StatusCode = HttpStatusCode.OK;
                response.IsSuccess = true;
                response.Result = list;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }
            return HandleResponse(response);
        }

        // POST /api/TrainingRegistrations (ลงทะเบียน)
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] TrainingRegistrationDto model)
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

                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    response.StatusCode = HttpStatusCode.Unauthorized;
                    response.IsSuccess = false;
                    response.ErrorMessages.Add("กรุณาเข้าสู่ระบบใหม่เพื่อทำรายการ");
                    return Unauthorized(response);
                }

                // 1. ตรวจสอบหลักสูตรอบรม
                var topic = await _context.Trainingtopics.FindAsync(model.TrainingtopicId);
                if (topic == null)
                {
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.IsSuccess = false;
                    response.ErrorMessages.Add("ไม่พบหัวข้ออบรมที่ต้องการสมัคร");
                    return NotFound(response);
                }

                // 2. เช็คเวลาสมัคร
                var now = DateTime.UtcNow;
                if (!topic.IsOpenRegister)
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.IsSuccess = false;
                    response.ErrorMessages.Add("หลักสูตรนี้ปิดรับสมัครแล้ว");
                    return BadRequest(response);
                }

                if (topic.RegisterStartDate.HasValue && topic.RegisterStartDate.Value > now)
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.IsSuccess = false;
                    response.ErrorMessages.Add("หลักสูตรนี้ยังไม่เปิดรับสมัคร");
                    return BadRequest(response);
                }

                if (topic.RegisterEndDate.HasValue && topic.RegisterEndDate.Value < now)
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.IsSuccess = false;
                    response.ErrorMessages.Add("หลักสูตรนี้หมดเขตรับสมัครแล้ว");
                    return BadRequest(response);
                }

                // 3. เช็คโควตาผู้เข้าร่วม
                var currentParticipantsCount = await _context.TrainingRegistrations
                    .CountAsync(r => r.TrainingtopicId == model.TrainingtopicId && r.Status != "ยกเลิกแล้ว");

                if (topic.MaxParticipants > 0 && currentParticipantsCount >= topic.MaxParticipants)
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.IsSuccess = false;
                    response.ErrorMessages.Add("จำนวนผู้สมัครเข้าร่วมอบรมเต็มแล้ว");
                    return BadRequest(response);
                }

                // 4. เช็คว่าเคยลงสมัครไปแล้วหรือยัง
                var alreadyRegistered = await _context.TrainingRegistrations
                    .AnyAsync(r => r.TrainingtopicId == model.TrainingtopicId && r.UserId == userId && r.Status != "ยกเลิกแล้ว");

                if (alreadyRegistered)
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.IsSuccess = false;
                    response.ErrorMessages.Add("คุณได้ลงทะเบียนเข้าร่วมอบรมหลักสูตรนี้ไปแล้ว");
                    return BadRequest(response);
                }

                // 5. บันทึกข้อมูล
                var reg = new TrainingRegistration
                {
                    TrainingtopicId = model.TrainingtopicId,
                    UserId = userId,
                    Prefix = model.Prefix,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    RegisteredAt = DateTime.UtcNow,
                    Status = "ลงทะเบียนแล้ว"
                };

                _context.TrainingRegistrations.Add(reg);
                await _context.SaveChangesAsync();

                response.StatusCode = HttpStatusCode.Created;
                response.IsSuccess = true;
                response.Result = reg;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }
            return HandleResponse(response);
        }

        // PUT /api/TrainingRegistrations/{id} (แก้ไขข้อมูลการลงทะเบียน)
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] TrainingRegistrationUpdateDto model)
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

                var userId = GetCurrentUserId();
                var reg = await _context.TrainingRegistrations
                    .Include(r => r.Trainingtopic)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (reg == null)
                {
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.IsSuccess = false;
                    response.ErrorMessages.Add("ไม่พบข้อมูลการลงทะเบียนอบรม");
                    return NotFound(response);
                }

                // เช็คสิทธิ์ความเป็นเจ้าของ
                if (reg.UserId != userId)
                {
                    response.StatusCode = HttpStatusCode.Forbidden;
                    response.IsSuccess = false;
                    response.ErrorMessages.Add("คุณไม่มีสิทธิ์แก้ไขรายการนี้");
                    return Forbid();
                }

                // เช็ควันหมดเขตรับสมัคร
                var now = DateTime.UtcNow;
                if (reg.Trainingtopic?.RegisterEndDate.HasValue == true && reg.Trainingtopic.RegisterEndDate.Value < now)
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.IsSuccess = false;
                    response.ErrorMessages.Add("หมดเขตรับสมัครและปิดรับการแก้ไขข้อมูลแล้ว");
                    return BadRequest(response);
                }

                reg.Prefix = model.Prefix;
                reg.FirstName = model.FirstName;
                reg.LastName = model.LastName;

                _context.TrainingRegistrations.Update(reg);
                await _context.SaveChangesAsync();

                response.StatusCode = HttpStatusCode.OK;
                response.IsSuccess = true;
                response.Result = reg;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }
            return HandleResponse(response);
        }

        // DELETE /api/TrainingRegistrations/{id} (ยกเลิกการลงทะเบียน - Soft Delete)
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var response = new ApiResponse();
            try
            {
                var userId = GetCurrentUserId();
                var reg = await _context.TrainingRegistrations
                    .Include(r => r.Trainingtopic)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (reg == null)
                {
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.IsSuccess = false;
                    response.ErrorMessages.Add("ไม่พบข้อมูลการลงทะเบียนอบรม");
                    return NotFound(response);
                }

                // เช็คสิทธิ์เจ้าของ
                if (reg.UserId != userId)
                {
                    response.StatusCode = HttpStatusCode.Forbidden;
                    response.IsSuccess = false;
                    response.ErrorMessages.Add("คุณไม่มีสิทธิ์ยกเลิกรายการนี้");
                    return Forbid();
                }

                // เช็ควันหมดเขตรับสมัคร
                var now = DateTime.UtcNow;
                if (reg.Trainingtopic?.RegisterEndDate.HasValue == true && reg.Trainingtopic.RegisterEndDate.Value < now)
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.IsSuccess = false;
                    response.ErrorMessages.Add("หมดเขตรับสมัครและปิดรับการยกเลิกข้อมูลแล้ว");
                    return BadRequest(response);
                }

                // Soft Delete: เปลี่ยนสถานะเป็น "ยกเลิกแล้ว"
                reg.Status = "ยกเลิกแล้ว";

                _context.TrainingRegistrations.Update(reg);
                await _context.SaveChangesAsync();

                response.StatusCode = HttpStatusCode.OK;
                response.IsSuccess = true;
                response.Result = reg;
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

    public class TrainingRegistrationDto
    {
        public int TrainingtopicId { get; set; }
        public string? Prefix { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }

    public class TrainingRegistrationUpdateDto
    {
        public string? Prefix { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }
}
