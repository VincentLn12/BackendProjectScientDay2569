using System.Security.Claims;
using System.Text.Json;

namespace ProjectScientDay2568.Service
{
    public static class AuditActions
    {
        public const string LoginSuccess = "Auth.LoginSuccess";
        public const string LoginFailed = "Auth.LoginFailed";
        public const string LoginBlockedSuspended = "Auth.LoginBlockedSuspended";
        public const string AccountLockedOut = "Auth.AccountLockedOut";
        public const string LoginBlockedLockedOut = "Auth.LoginBlockedLockedOut";
        public const string PasswordResetCompleted = "Auth.PasswordResetCompleted";
        public const string PasswordResetFailed = "Auth.PasswordResetFailed";
        public const string UserRoleChanged = "User.RoleChanged";
        public const string UserSuspended = "User.Suspended";
        public const string UserUnsuspended = "User.Unsuspended";
        public const string ResetLinkCreated = "User.ResetLinkCreated";
    }

    public class AuditLogService
    {
        private readonly ApplicationDbContext _db;
        private readonly IHttpContextAccessor _http;
        private readonly ILogger<AuditLogService> _logger;

        public AuditLogService(ApplicationDbContext db, IHttpContextAccessor http, ILogger<AuditLogService> logger)
        {
            _db = db;
            _http = http;
            _logger = logger;
        }

        private static string? Cut(string? s, int max) => s == null ? null : (s.Length <= max ? s : s[..max]);

        /// <summary>
        /// บันทึก log หนึ่งรายการ ไม่โยน exception (log พังต้องไม่ทำให้งานหลักพัง)
        /// actor: ถ้าไม่ระบุ ใช้ผู้ใช้ที่ล็อกอินอยู่ (เช่นตอน login ยังไม่มี จึงส่งชื่อที่พยายามเข้ามาเอง)
        /// </summary>
        public async Task LogAsync(string action, string? target = null, object? details = null, string? actor = null)
        {
            try
            {
                var ctx = _http.HttpContext;
                actor ??= ctx?.User.FindFirst(ClaimTypes.Email)?.Value ?? ctx?.User.Identity?.Name;
                _db.AuditLogs.Add(new AuditLog
                {
                    CreatedAt = DateTime.UtcNow,
                    Actor = Cut(actor, 256),
                    Action = Cut(action, 100)!,
                    Target = Cut(target, 256),
                    Details = details == null ? null : Cut(JsonSerializer.Serialize(details), 2000),
                    IpAddress = Cut(ctx?.Connection.RemoteIpAddress?.ToString(), 64),
                });
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write audit log {Action}", action);
            }
        }
    }
}
