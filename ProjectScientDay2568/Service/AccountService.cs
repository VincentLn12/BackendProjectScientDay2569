using ProjectScientDay2568.Utility;
using static ProjectScientDay2568.Models.Dtos.Auth.AuthDtos;

namespace ProjectScientDay2568.Service
{
    public class AccountService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IHttpContextAccessor _http;

        public AccountService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _http = httpContextAccessor;
        }

        public async Task<(bool ok, UserDto? user, IEnumerable<string> errors)> RegisterAsync(RegisterDto dto)
        {
            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                return (false, null, result.Errors.Select(e => e.Description));

            // ✅ ใส่ role user ใหม่ทุกครั้ง
            if (!await _userManager.IsInRoleAsync(user, SD.Role_User))
                await _userManager.AddToRoleAsync(user, SD.Role_User);

            await _signInManager.SignInAsync(user, isPersistent: false);

            var roles = await _userManager.GetRolesAsync(user);

            return (true,
                new UserDto(user.Id, user.Email, roles.ToArray()),
                Array.Empty<string>());
        }

        public async Task<(bool ok, UserDto? user, string? error)> LoginAsync(LoginDto dto)
        {
            // ลองหาเป็นอีเมลก่อน ไม่เจอค่อยหาเป็นยูสเนม
            var user = await _userManager.FindByEmailAsync(dto.Email)
                   ?? await _userManager.FindByNameAsync(dto.Email);

            if (user is null) return (false, null, "Invalid credentials");

            var result = await _signInManager.PasswordSignInAsync(
                user, dto.Password, isPersistent: false, lockoutOnFailure: true);

            if (!result.Succeeded) return (false, null, "Invalid credentials");

            var roles = await _userManager.GetRolesAsync(user);
            return (true, new UserDto(user.Id, user.Email, roles.ToArray()), null);
        }


        public async Task<UserDto?> GetCurrentUserAsync()
        {
            var principal = _http.HttpContext?.User;
            if (principal?.Identity?.IsAuthenticated != true) return null;

            var user = await _userManager.GetUserAsync(principal);
            if (user is null) return null;

            var roles = await _userManager.GetRolesAsync(user);

            return new UserDto(user.Id, user.Email, roles.ToArray());
        }

        public async Task LogoutAsync() => await _signInManager.SignOutAsync();

        public async Task<(bool ok, IEnumerable<string> errors)> ChangePasswordAsync(ChangePasswordDto dto)
        {
            var user = await _userManager.GetUserAsync(_http.HttpContext!.User);
            if (user is null) return (false, new[] { "Unauthorized" });

            var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
            return result.Succeeded
                ? (true, Array.Empty<string>())
                : (false, result.Errors.Select(e => e.Description));
        }
    }
}
