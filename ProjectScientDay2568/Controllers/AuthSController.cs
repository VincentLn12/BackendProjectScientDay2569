//using Microsoft.AspNetCore.Antiforgery;
//using Microsoft.AspNetCore.Authorization;

//using static ProjectScientDay2568.Models.Dtos.Auth.AuthDtos;

//namespace ProjectScientDay2568.Controllers
//{
//    [Route("auth")] 
//    [ApiController]
//    public class AuthSController : ControllerBase
//    {
//        private readonly IAntiforgery _anti;
//        private readonly AccountService _account;
//        private readonly SignInManager<ApplicationUser> _signInManager;

//        public AuthSController(IAntiforgery anti, AccountService account , SignInManager<ApplicationUser> signInManager)
//        {
//            _anti = anti;
//            _account = account;
//            _signInManager = signInManager;
//        }

//        // 1) CSRF: ออกคู่โทเค็น (cookie XSRF-TOKEN + body.csrfToken)
//        [HttpGet("csrf")]
//        [AllowAnonymous]
//        public IActionResult GetCsrf()
//        {
//            var tokens = _anti.GetAndStoreTokens(HttpContext);
//            return Ok(new { csrfToken = tokens.RequestToken });
//        }

//        // 2) Register
//        [HttpPost("register")]
//        [AllowAnonymous]
//        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
//        {
//            await _anti.ValidateRequestAsync(HttpContext);
//            var (ok, user, errors) = await _account.RegisterAsync(dto);
//            if (!ok) return BadRequest(new { message = "Register failed", errors });
//            return Ok(user);
//        }

//        // 3) Login
//        [HttpPost("login")]
//        [AllowAnonymous]
//        public async Task<IActionResult> Login([FromBody] LoginDto dto)
//        {
//            await _anti.ValidateRequestAsync(HttpContext);
//            var (ok, user, error) = await _account.LoginAsync(dto);
//            if (!ok || user is null) return Unauthorized(new { message = error ?? "Invalid credentials" });
//            return Ok(user);
//        }

//        // 4) Me (ต้อง Auth)
//        [HttpGet("me")]
//        [Authorize]
//        public async Task<IActionResult> Me()
//        {
//            var user = await _account.GetCurrentUserAsync();
//            return user is null ? Unauthorized() : Ok(user);
//        }

//        // 5) Logout (ต้อง Auth)
//        [HttpPost("logout")]
//        [Authorize(Policy = "JwtOrCookie")]
//        public async Task<IActionResult> Logout([FromServices] IAntiforgery anti)
//        {
//            // ถ้าใช้ CSRF กับ cookie: ตรวจให้ผ่านก่อน
//            await anti.ValidateRequestAsync(HttpContext);

//            await _signInManager.SignOutAsync();

//            // ลบ APP_AUTH แบบ manual เผื่อกรณีบางเบราว์เซอร์
//            Response.Cookies.Append("APP_AUTH", string.Empty, new CookieOptions
//            {
//                Expires = DateTimeOffset.UnixEpoch,
//                HttpOnly = true,
//                Secure = true,
//                SameSite = SameSiteMode.None,
//                Path = "/"
//                // ถ้าตอนตั้งมี Domain=xxx ก็ควรใส่ Domain เดิมด้วย
//                // Domain = ".yourdomain.local"
//            });

//            // ลบ XSRF-TOKEN ด้วย (จะออก token ใหม่หลัง logout)
//            Response.Cookies.Delete("XSRF-TOKEN", new CookieOptions
//            {
//            {
//                Secure = true,
//                SameSite = SameSiteMode.None,
//                Path = "/"
//                // Domain = ".yourdomain.local" // ถ้ามีตอนตั้ง
//            });

//            // กัน cache หน้าเดิม
//            Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
//            Response.Headers["Pragma"] = "no-cache";

//            return Ok(new { message = "logged out" });
//        }

//    }
//}
