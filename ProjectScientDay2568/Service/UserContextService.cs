namespace ProjectScientDay2568.Service
{
    public class UserContextService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserContextService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string UserId
        {
            get
            {
                var user = _httpContextAccessor.HttpContext?.User;
                var userId = user?.FindFirst("id")?.Value;

                return userId ?? throw new Exception("ไม่พบรหัสผู้ใช้");
            }
        }

        public string UserRole
        {
            get
            {
                var user = _httpContextAccessor.HttpContext?.User;
                var role = user?.FindFirst("role")?.Value
                    ?? user?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
                    ?? user?.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value;

                return role ?? "";
            }
        }

        public bool IsStaffOrAdmin
        {
            get
            {
                var role = UserRole?.ToLower() ?? "";
                return role == "admin" || role == "staff" || role == "เจ้าหน้าที่" || role == "ผู้ดูแลระบบ" || role.Contains("admin") || role.Contains("staff");
            }
        }
    }
}
