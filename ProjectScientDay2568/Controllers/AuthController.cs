using Microsoft.AspNetCore.RateLimiting;
using ProjectScientDay2568.Helper;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.Tokens;
using ProjectScientDay2568.Models.Dtos.Auth;
using ProjectScientDay2568.Utility;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using static ProjectScientDay2568.Models.Dtos.Auth.ResetPasswordDtos;

namespace ProjectScientDay2568.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private ApiResponse _response;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private string secretKey;

        public AuthController(ApplicationDbContext db, IConfiguration configuration,
           UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _db = db;
            secretKey = configuration.GetValue<string>("ApiSettings:Secret");
            _response = new ApiResponse();
            _userManager = userManager;
            _roleManager = roleManager;
        }

        private IActionResult HandleResponse(ApiResponse response)
          => StatusCode((int)response.StatusCode, response);

        [HttpPost("register")]
        [EnableRateLimiting(RateLimitPolicies.Sensitive)]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDTO model)
        {
            ApplicationUser userFromDb = await _db.ApplicationUsers
                .FirstOrDefaultAsync(u => u.UserName.ToLower() == model.UserName.ToLower());

            if (userFromDb != null)
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add("Username already exists");
                return BadRequest(_response);
            }

            ApplicationUser newUser = new()
            {
                UserName = model.UserName,
                Email = model.UserName,
                NormalizedEmail = model.UserName.ToUpper(),
         
            };

            try
            {
                var result = await _userManager.CreateAsync(newUser, model.Password);
                if (result.Succeeded)
                {
                    if (!_roleManager.RoleExistsAsync(SD.Role_Admin).GetAwaiter().GetResult())
                    {
                        //create roles in database
                        await _roleManager.CreateAsync(new IdentityRole(SD.Role_Admin));
                        await _roleManager.CreateAsync(new IdentityRole(SD.Role_User));
                    }
                    if (model.Role.ToLower() == SD.Role_Admin)
                    {
                        await _userManager.AddToRoleAsync(newUser, SD.Role_Admin);
                    }
                    else
                    {
                        await _userManager.AddToRoleAsync(newUser, SD.Role_User);
                    }

                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = true;
                    return Ok(_response);
                }
            }
            catch (Exception)
            {

            }

            _response.StatusCode = HttpStatusCode.BadRequest;
            _response.IsSuccess = false;
            _response.ErrorMessages.Add("Error while registering");
            return BadRequest(_response);
        }

        [HttpPost("login")]
        [EnableRateLimiting(RateLimitPolicies.Login)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDTO model, [FromServices] AuditLogService audit)
        {
            ApplicationUser userFromDb = await _db.ApplicationUsers
                    .FirstOrDefaultAsync(u => u.UserName.ToLower() == model.UserName.ToLower());

            if (userFromDb != null && !AccountSuspension.IsSuspended(userFromDb) && await _userManager.IsLockedOutAsync(userFromDb))
            {
                var minutes = Math.Max(1, (int)Math.Ceiling((userFromDb.LockoutEnd!.Value - DateTimeOffset.UtcNow).TotalMinutes));
                await audit.LogAsync(AuditActions.LoginBlockedLockedOut, userFromDb.UserName, actor: userFromDb.UserName);
                _response.Result = new LoginResponseDTO();
                _response.StatusCode = HttpStatusCode.TooManyRequests;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add($"ใส่รหัสผ่านผิดหลายครั้ง บัญชีถูกล็อกชั่วคราว กรุณารอ {minutes} นาทีแล้วลองใหม่");
                return StatusCode((int)HttpStatusCode.TooManyRequests, _response);
            }

            bool isValid = userFromDb != null && await _userManager.CheckPasswordAsync(userFromDb, model.Password);
            if (userFromDb != null && !AccountSuspension.IsSuspended(userFromDb))
            {
                if (isValid) await _userManager.ResetAccessFailedCountAsync(userFromDb);
                else
                {
                    await _userManager.AccessFailedAsync(userFromDb);
                    if (await _userManager.IsLockedOutAsync(userFromDb))
                        await audit.LogAsync(AuditActions.AccountLockedOut, userFromDb.UserName, actor: userFromDb.UserName);
                }
            }

            if (isValid == false)
            {
                await audit.LogAsync(AuditActions.LoginFailed, model.UserName, actor: model.UserName);
                _response.Result = new LoginResponseDTO();
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add("Username or password is incorrect");
                return BadRequest(_response);
            }

            if (AccountSuspension.IsSuspended(userFromDb!))
            {
                await audit.LogAsync(AuditActions.LoginBlockedSuspended, userFromDb.UserName, actor: userFromDb.UserName);
                _response.Result = new LoginResponseDTO();
                _response.StatusCode = HttpStatusCode.Forbidden;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add("บัญชีนี้ถูกระงับการใช้งาน กรุณาติดต่อผู้ดูแลระบบ");
                return StatusCode((int)HttpStatusCode.Forbidden, _response);
            }

            //we have to generate JWT Token

            var roles = await _userManager.GetRolesAsync(userFromDb);
            JwtSecurityTokenHandler tokenHandler = new();
            byte[] key = Encoding.ASCII.GetBytes(secretKey);

            SecurityTokenDescriptor tokenDescriptor = new()
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim("id", userFromDb.Id.ToString()),
                    new Claim(ClaimTypes.Email, userFromDb.UserName.ToString()),
                    new Claim(ClaimTypes.Role, roles.FirstOrDefault()),
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);

            LoginResponseDTO loginResponse = new()
            {
                Email = userFromDb.Email,
                Token = tokenHandler.WriteToken(token)
            };

            if (loginResponse.Email == null || string.IsNullOrEmpty(loginResponse.Token))
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add("Username or password is incorrect");
                return BadRequest(_response);
            }

            await audit.LogAsync(AuditActions.LoginSuccess, userFromDb.UserName, new { role = roles.FirstOrDefault() }, actor: userFromDb.UserName);
            _response.StatusCode = HttpStatusCode.OK;
            _response.IsSuccess = true;
            _response.Result = loginResponse;
            return Ok(_response);
        }

        [HttpPost("reset-password")]
        [EnableRateLimiting(RateLimitPolicies.Sensitive)]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest dto, [FromServices] AuditLogService audit)
        {
            var res = new ApiResponse();
            try
            {
                var user = await _userManager.FindByIdAsync(dto.UserId);
                if (user == null)
                {
                    // เพื่อความปลอดภัย อาจคืน 200 เฉย ๆ ก็ได้ ไม่บอกว่ามี user นี้ไหม
                    res.IsSuccess = false;
                    res.StatusCode = HttpStatusCode.NotFound;
                    res.ErrorMessages.Add("ไม่พบผู้ใช้");
                    return HandleResponse(res);
                }

                // ถอดรหัส token
                var token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(dto.Token));

                // รีเซ็ตรหัส
                var result = await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);
                if (!result.Succeeded)
                {
                    await audit.LogAsync(AuditActions.PasswordResetFailed, user.UserName,
                        new { reason = string.Join(", ", result.Errors.Select(e => e.Code)) }, actor: user.UserName);
                    res.IsSuccess = false;
                    res.StatusCode = HttpStatusCode.BadRequest;
                    res.ErrorMessages.AddRange(result.Errors.Select(e => e.Description));
                    return HandleResponse(res);
                }

                // (แนะนำ) บังคับ logout ทุกเครื่อง: update security stamp
                await _userManager.UpdateSecurityStampAsync(user);
                await audit.LogAsync(AuditActions.PasswordResetCompleted, user.UserName, actor: user.UserName);

                res.StatusCode = HttpStatusCode.OK;
                res.Result = new { message = "รีเซ็ตรหัสสำเร็จ" };
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