using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectScientDay2568.Data;
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
        private readonly ApplicationDbContext _context;

        public TrainingtopicsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private IActionResult HandleResponse(ApiResponse response)
        {
            return StatusCode((int)response.StatusCode, response);
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var response = new ApiResponse();
            try
            {
                var topics = await _context.Trainingtopics
                    .Include(t => t.branch)
                    .Include(t => t.CertificateTemplate)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();

                foreach (var t in topics)
                {
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
                var topic = await _context.Trainingtopics
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
                    CertificateTemplateId = model.CertificateTemplateId
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

                _context.Trainingtopics.Remove(topic);
                await _context.SaveChangesAsync();

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
    }
}
