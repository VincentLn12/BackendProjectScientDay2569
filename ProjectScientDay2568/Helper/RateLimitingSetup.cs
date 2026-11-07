using System.Security.Claims;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;

namespace ProjectScientDay2568.Helper
{
    // ชื่อ policy ที่ใช้กับ [EnableRateLimiting(...)] บน controller
    public static class RateLimitPolicies
    {
        public const string Login = "login";
        public const string Sensitive = "sensitive"; // สมัครสมาชิก / ตั้งรหัสใหม่ / สร้างลิงก์รีเซ็ต
        public const string Upload = "upload";
    }

    // ค่าตั้งจาก appsettings: "RateLimiting": { ... } (ต่อ 1 นาที)
    public class RateLimitingOptions
    {
        public bool Enabled { get; set; } = true;
        public int LoginPerMinute { get; set; } = 10;
        public int SensitivePerMinute { get; set; } = 5;
        public int UploadPerMinute { get; set; } = 20;
        public int UserPerMinute { get; set; } = 300;       // ผู้ที่ล็อกอินแล้ว นับต่อบัญชี
        public int AnonymousPerMinute { get; set; } = 600;  // ยังไม่ล็อกอิน นับต่อ IP (หลวม เพราะทั้งโรงเรียนอาจใช้ IP เดียว)
    }

    public static class RateLimitingSetup
    {
        private static string ClientIp(HttpContext ctx) => ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        private static string? UserId(HttpContext ctx) =>
            ctx.User.Identity?.IsAuthenticated == true ? ctx.User.FindFirst("id")?.Value ?? ctx.User.FindFirst(ClaimTypes.Email)?.Value : null;

        private static RateLimitPartition<string> PerMinute(string key, int limit) =>
            RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = Math.Max(1, limit),
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            });

        public static IServiceCollection AddAppRateLimiting(this IServiceCollection services, IConfiguration config)
        {
            var opt = config.GetSection("RateLimiting").Get<RateLimitingOptions>() ?? new RateLimitingOptions();

            // อยู่หลัง reverse proxy (nginx / ARR) ต้องอ่าน IP จริงจาก X-Forwarded-For
            // เปิดเฉพาะเมื่อมี proxy จริง ไม่งั้นคนภายนอกปลอม header เพื่อหลบการจำกัดได้
            if (config.GetValue<bool>("ReverseProxy:TrustForwardedHeaders"))
            {
                services.Configure<ForwardedHeadersOptions>(o =>
                {
                    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                    o.KnownNetworks.Clear();
                    o.KnownProxies.Clear();
                });
            }

            services.AddRateLimiter(o =>
            {
                o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                o.OnRejected = async (ctx, ct) =>
                {
                    var wait = ctx.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry) ? (int)Math.Ceiling(retry.TotalSeconds) : 60;
                    ctx.HttpContext.Response.Headers.RetryAfter = wait.ToString();
                    ctx.HttpContext.Response.ContentType = "application/json; charset=utf-8";
                    var body = new ApiResponse { IsSuccess = false, StatusCode = HttpStatusCode.TooManyRequests };
                    body.ErrorMessages.Add($"ส่งคำขอบ่อยเกินไป กรุณารอประมาณ {Math.Max(1, wait)} วินาทีแล้วลองใหม่");
                    await ctx.HttpContext.Response.WriteAsync(
                        JsonSerializer.Serialize(body, new JsonSerializerOptions(JsonSerializerDefaults.Web)), ct);
                };

                if (!opt.Enabled) return;

                // ทุก API: ล็อกอินแล้วนับต่อบัญชี ยังไม่ล็อกอินนับต่อ IP
                // ไม่จำกัด SignalR (/matchHub) และไฟล์หน้าเว็บ/รูป (ไม่ได้ขึ้นต้นด้วย /api)
                o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
                {
                    if (!ctx.Request.Path.StartsWithSegments("/api")) return RateLimitPartition.GetNoLimiter("static");
                    var uid = UserId(ctx);
                    return uid != null
                        ? PerMinute("u:" + uid, opt.UserPerMinute)
                        : PerMinute("ip:" + ClientIp(ctx), opt.AnonymousPerMinute);
                });

                o.AddPolicy(RateLimitPolicies.Login, ctx => PerMinute("login:" + ClientIp(ctx), opt.LoginPerMinute));
                o.AddPolicy(RateLimitPolicies.Sensitive, ctx => PerMinute("sens:" + ClientIp(ctx), opt.SensitivePerMinute));
                o.AddPolicy(RateLimitPolicies.Upload, ctx => PerMinute("up:" + (UserId(ctx) ?? ClientIp(ctx)), opt.UploadPerMinute));
            });

            return services;
        }
    }
}
