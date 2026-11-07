using Microsoft.AspNetCore.RateLimiting;
using ProjectScientDay2568.Helper;
using Microsoft.AspNetCore.Authorization;
using ProjectScientDay2568.Models.Dtos.Auth;
using Microsoft.AspNetCore.WebUtilities;
using static ProjectScientDay2568.Models.Dtos.Auth.ResetPasswordDtos;
using System.Text;

namespace ProjectScientDay2568.Controllers
{
    [Route("api/admin/users")]
    [Authorize(Roles = "Admin,admin")]
    [ApiController]
    public class AdminUsersController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminUsersController(
            UserService userService,
            RoleManager<IdentityRole> roleManager,
            UserManager<ApplicationUser> userManager)
        {
            _userService = userService;
            _roleManager = roleManager;
            _userManager = userManager;
        }

        private IActionResult HandleResponse(ApiResponse response)
            => StatusCode((int)response.StatusCode, response);

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var response = new ApiResponse();
            try
            {
                var roles = await _userService.GetAllUsersWithRolesAsync();
                response.StatusCode = HttpStatusCode.OK;
                response.Result = roles;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }
            return HandleResponse(response);
        }

        [HttpGet("roles")]
        public async Task<IActionResult> GetRoles()
        {
            var list = await _roleManager.Roles
                .Select(r => new RoleDto {  Name = r.Name! })
                .ToListAsync();

            var res = new ApiResponse
            {
                StatusCode = HttpStatusCode.OK,
                Result = list
            };
            return HandleResponse(res);
        }

        [HttpPut("{userName}/role")]
        public async Task<IActionResult> ChangeRole([FromRoute] string userName, [FromBody] ChangeRoleBody dto, [FromServices] AuditLogService audit)
        {
            var res = new ApiResponse();
            try
            {
                if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(dto?.NewRole))
                {
                    res.IsSuccess = false;
                    res.StatusCode = HttpStatusCode.BadRequest;
                    res.ErrorMessages.Add("ต้องระบุ userName ใน route และ NewRole ใน body");
                    return HandleResponse(res);
                }

                var user = await _userManager.FindByNameAsync(userName);
                if (user == null)
                {
                    res.IsSuccess = false;
                    res.StatusCode = HttpStatusCode.NotFound;
                    res.ErrorMessages.Add($"ไม่พบผู้ใช้ '{userName}'");
                    return HandleResponse(res);
                }

                if (!await _roleManager.RoleExistsAsync(dto.NewRole))
                {
                    res.IsSuccess = false;
                    res.StatusCode = HttpStatusCode.BadRequest;
                    res.ErrorMessages.Add($"บทบาท '{dto.NewRole}' ไม่มีอยู่ในระบบ");
                    return HandleResponse(res);
                }

                // กัน admin คนสุดท้าย (เหมือนเดิม)
                if (!string.Equals(dto.NewRole, "Admin", StringComparison.OrdinalIgnoreCase))
                {
                    var admins = await _userManager.GetUsersInRoleAsync("Admin");
                    var isSelf = string.Equals(User.Identity?.Name, userName, StringComparison.OrdinalIgnoreCase);
                    if (admins.Count == 1 && admins.Any(a => a.UserName == userName) && isSelf)
                    {
                        res.IsSuccess = false;
                        res.StatusCode = HttpStatusCode.Forbidden;
                        res.ErrorMessages.Add("ไม่สามารถลดสิทธิ์ตัวเองได้ เนื่องจากคุณเป็น Admin คนสุดท้าย");
                        return HandleResponse(res);
                    }
                }

                var oldRoles = await _userManager.GetRolesAsync(user);
                var ok = await _userService.ChangeUserRoleAsync(userName, dto.NewRole);
                if (!ok)
                {
                    res.IsSuccess = false;
                    res.StatusCode = HttpStatusCode.BadRequest;
                    res.ErrorMessages.Add("เปลี่ยนบทบาทไม่สำเร็จ");
                    return HandleResponse(res);
                }

                var updatedRoles = await _userManager.GetRolesAsync(user);
                await audit.LogAsync(AuditActions.UserRoleChanged, user.UserName,
                    new { from = string.Join(", ", oldRoles), to = string.Join(", ", updatedRoles) });
                var payload = new UserRoleViewModel
                {
                    UserName = user.UserName!,
                    Email = user.Email!,
                    CurrentRoles = string.Join(", ", updatedRoles),
                    SelectedRole = updatedRoles.FirstOrDefault(),
                };

                res.StatusCode = HttpStatusCode.OK;
                res.Result = payload;
                return HandleResponse(res);
            }
            catch (Exception ex)
            {
                res.IsSuccess = false;
                res.StatusCode = HttpStatusCode.InternalServerError;
                res.ErrorMessages.Add(ex.Message);
                return HandleResponse(res);
            }
        }
        // เฉพาะแอดมิน (ตาม [Authorize] ของ class) เพราะคืนลิงก์ให้นำไปส่งต่อเอง
        // ระงับ (suspended = true) หรือเปิดใช้งานคืน (false) โดยใช้ LockoutEnd ของ Identity
        [HttpPut("{userName}/suspend")]
        public async Task<IActionResult> SetSuspended([FromRoute] string userName, [FromBody] SetSuspendedBody body, [FromServices] AuditLogService audit)
        {
            var res = new ApiResponse();
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null)
            {
                res.IsSuccess = false;
                res.StatusCode = HttpStatusCode.NotFound;
                res.ErrorMessages.Add($"ไม่พบผู้ใช้ '{userName}'");
                return HandleResponse(res);
            }

            if (body.Suspended)
            {
                if (string.Equals(User.FindFirst("id")?.Value, user.Id, StringComparison.Ordinal))
                {
                    res.IsSuccess = false;
                    res.StatusCode = HttpStatusCode.BadRequest;
                    res.ErrorMessages.Add("ไม่สามารถระงับบัญชีของตัวเองได้");
                    return HandleResponse(res);
                }

                if (await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    var activeAdmins = (await _userManager.GetUsersInRoleAsync("Admin"))
                        .Count(a => !AccountSuspension.IsSuspended(a));
                    if (activeAdmins <= 1)
                    {
                        res.IsSuccess = false;
                        res.StatusCode = HttpStatusCode.BadRequest;
                        res.ErrorMessages.Add("ไม่สามารถระงับผู้ดูแลระบบคนสุดท้ายได้");
                        return HandleResponse(res);
                    }
                }

                await _userManager.SetLockoutEnabledAsync(user, true);
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            }
            else
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
                await _userManager.ResetAccessFailedCountAsync(user);
            }

            await audit.LogAsync(body.Suspended ? AuditActions.UserSuspended : AuditActions.UserUnsuspended, user.UserName);
            res.StatusCode = HttpStatusCode.OK;
            res.Result = new { userName = user.UserName, isSuspended = body.Suspended };
            return HandleResponse(res);
        }

        [HttpPost("{userName}/reset-password-link")]
        [EnableRateLimiting(RateLimitPolicies.Sensitive)]
        public async Task<IActionResult> GenerateResetLink([FromRoute] string userName, [FromBody] GenerateResetLinkBody? body, [FromServices] IConfiguration config, [FromServices] AuditLogService audit)
        {
            var res = new ApiResponse();
            try
            {
                var user = await _userManager.FindByNameAsync(userName);

                if (user == null)
                {
                    res.IsSuccess = false;
                    res.StatusCode = HttpStatusCode.NotFound;
                    res.ErrorMessages.Add($"ไม่พบผู้ใช้ '{userName}'");
                    return HandleResponse(res);
                }

                // ที่อยู่หน้าเว็บ: ใช้ค่าใน appsettings (Frontend:BaseUrl) ถ้ามี
                // ไม่งั้นใช้ที่อยู่ที่หน้าเว็บส่งมา (ต้องเป็นโดเมนเดียวกับ Origin ของคำขอ กันการปลอมลิงก์)
                var feBase = config["Frontend:BaseUrl"]?.TrimEnd('/');
                if (string.IsNullOrEmpty(feBase))
                {
                    var origin = Request.Headers.Origin.ToString();
                    var client = body?.ClientBaseUrl?.TrimEnd('/');
                    if (string.IsNullOrEmpty(client) || string.IsNullOrEmpty(origin)
                        || !client.StartsWith(origin, StringComparison.OrdinalIgnoreCase))
                    {
                        res.IsSuccess = false;
                        res.StatusCode = HttpStatusCode.BadRequest;
                        res.ErrorMessages.Add("ไม่ทราบที่อยู่หน้าเว็บ กรุณาตั้งค่า Frontend:BaseUrl ใน appsettings");
                        return HandleResponse(res);
                    }
                    feBase = client;
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                // URL-safe
                var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

                // สร้าง URL อย่างปลอดภัยด้วย QueryHelpers (กันเครื่องหมายเพี้ยน)
                var resetUrl = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(
                    $"{feBase}/reset-password",
                    new Dictionary<string, string?>
                    {
                        ["uid"] = user.Id,
                        ["token"] = encoded
                    });

                // อายุโทเค็น ควรอิงค่าเดียวกับ Program.cs (DataProtectionTokenProviderOptions.TokenLifespan)
                // หรืออ่านจาก config เพื่อไม่ hardcode
                var expiresAt = DateTimeOffset.UtcNow.AddHours(2);

                await audit.LogAsync(AuditActions.ResetLinkCreated, user.UserName);

                // คืนลิงก์ทุกโหมด (รวม Release บน server) เพราะ endpoint นี้ใช้ได้เฉพาะแอดมิน
                res.StatusCode = HttpStatusCode.OK;
                res.Result = new
                {
                    userId = user.Id,
                    resetUrl,
                    expiresAt
                };
                return HandleResponse(res);
            }
            catch (Exception ex)
            {
                res.IsSuccess = false;
                res.StatusCode = HttpStatusCode.InternalServerError;
                res.ErrorMessages.Add(ex.Message);
                return HandleResponse(res);
            }
        }




    }

    public class SetSuspendedBody
    {
        public bool Suspended { get; set; }
    }

    public class GenerateResetLinkBody
    {
        public string? ClientBaseUrl { get; set; }
    }
}
