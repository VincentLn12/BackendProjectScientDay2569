using Microsoft.AspNetCore.Authorization;

namespace ProjectScientDay2568.Controllers
{
    // ดู audit log ได้อย่างเดียว (ไม่มี POST/PUT/DELETE) เฉพาะแอดมิน
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,admin")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class AuditLogsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public AuditLogsController(ApplicationDbContext db) { _db = db; }

        /// <param name="from">วันที่เริ่ม (เวลาไทย, yyyy-MM-dd)</param>
        /// <param name="to">วันที่สิ้นสุด (เวลาไทย, รวมทั้งวัน)</param>
        /// <param name="action">กรองตาม action หรือกลุ่ม เช่น "Auth." หรือ "User.Suspended"</param>
        /// <param name="q">ค้นหาในผู้กระทำ / เป้าหมาย / IP</param>
        [HttpGet]
        public async Task<IActionResult> Get(
            DateTime? from, DateTime? to, string? action, string? q, int page = 1, int pageSize = 50)
        {
            pageSize = Math.Clamp(pageSize, 10, 200);
            page = Math.Max(1, page);
            var thai = TimeSpan.FromHours(7);

            var query = _db.AuditLogs.AsNoTracking().AsQueryable();
            if (from.HasValue) query = query.Where(x => x.CreatedAt >= from.Value.Date - thai);
            if (to.HasValue) query = query.Where(x => x.CreatedAt < to.Value.Date.AddDays(1) - thai);
            if (!string.IsNullOrWhiteSpace(action))
                query = action.EndsWith('.') ? query.Where(x => x.Action.StartsWith(action)) : query.Where(x => x.Action == action);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(x => (x.Actor != null && x.Actor.Contains(term))
                    || (x.Target != null && x.Target.Contains(term))
                    || (x.IpAddress != null && x.IpAddress.Contains(term)));
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new
                {
                    x.Id,
                    CreatedAt = DateTime.SpecifyKind(x.CreatedAt, DateTimeKind.Utc),
                    x.Actor,
                    x.Action,
                    x.Target,
                    x.Details,
                    x.IpAddress,
                })
                .ToListAsync();

            return Ok(new ApiResponse { StatusCode = HttpStatusCode.OK, Result = new { total, page, pageSize, items } });
        }
    }
}
