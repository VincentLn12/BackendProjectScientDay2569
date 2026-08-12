using Microsoft.AspNetCore.Authorization;
using ProjectScientDay2568.Models.Dtos.Auth;
using Microsoft.AspNetCore.WebUtilities;
using static ProjectScientDay2568.Models.Dtos.Auth.ResetPasswordDtos;
using System.Text;

namespace ProjectScientDay2568.Controllers
{
    [Route("api/admin/users")]
    [Authorize(Roles = "admin")]
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
        public async Task<IActionResult> ChangeRole([FromRoute] string userName, [FromBody] ChangeRoleBody dto)
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

                var ok = await _userService.ChangeUserRoleAsync(userName, dto.NewRole);
                if (!ok)
                {
                    res.IsSuccess = false;
                    res.StatusCode = HttpStatusCode.BadRequest;
                    res.ErrorMessages.Add("เปลี่ยนบทบาทไม่สำเร็จ");
                    return HandleResponse(res);
                }

                var updatedRoles = await _userManager.GetRolesAsync(user);
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
        [HttpPost("{userName}/reset-password-link")]
        [AllowAnonymous] // หรือ [Authorize] ตามนโยบายคุณ
        public async Task<IActionResult> GenerateResetLink([FromRoute] string userName, [FromServices] IConfiguration config)
        {
            var res = new ApiResponse();
            try
            {
                var user = await _userManager.FindByNameAsync(userName);

                // ไม่ว่าพบ/ไม่พบ → ตอบ 200 เหมือนกัน เพื่อลด user enumeration
                if (user == null)
                {
                    res.StatusCode = HttpStatusCode.OK;
                    res.Result = new { message = "ถ้าบัญชีนี้ถูกต้อง ระบบได้ส่งลิงก์ให้แล้ว" };
                    return HandleResponse(res);
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                // URL-safe
                var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

                // สร้าง URL อย่างปลอดภัยด้วย QueryHelpers (กันเครื่องหมายเพี้ยน)
                //var feBase = (config["Frontend:BaseUrl"] ?? "http://localhost:5173").TrimEnd('/');
                var feBase = (config["Frontend:BaseUrl"] ?? "http://10.103.0.17/cs67/react/s14/rovmn").TrimEnd('/');

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

                // TODO: ปกติควร "ส่งอีเมล" ลิงก์นี้ไปยัง user.Email มากกว่าคืน link ตรง ๆ
                res.StatusCode = HttpStatusCode.OK;
                res.Result = new
                {
                    message = "ถ้าบัญชีนี้ถูกต้อง ระบบได้ส่งลิงก์ให้แล้ว",
                    // dev mode เท่านั้นค่อยคืน link (prod ปิด)
                    #if DEBUG
                    resetUrl,
                    expiresAt
                    #endif
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
}
