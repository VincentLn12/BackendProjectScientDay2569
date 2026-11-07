using ProjectScientDay2568.Models.Dtos.Auth;

namespace ProjectScientDay2568.Service
{
    public class UserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _db;

        public UserService(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext db)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _db = db;
        }

        public async Task<List<UserRoleViewModel>> GetAllUsersWithRolesAsync()
        {
            var users = _userManager.Users.ToList();
            var roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();

            var list = new List<UserRoleViewModel>();

            foreach (var user in users)
            {
                var userRoles = await _userManager.GetRolesAsync(user);
                list.Add(new UserRoleViewModel
                {
                    UserName = user.UserName,
                    Email = user.Email,
                    CurrentRoles = string.Join(", ", userRoles),
                    SelectedRole = userRoles.FirstOrDefault(),
                    IsSuspended = ProjectScientDay2568.Helper.AccountSuspension.IsSuspended(user),
                });
            }

            return list;
        }

        public async Task<bool> ChangeUserRoleAsync(string userName, string newRole)
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null) return false;

            var currentRoles = await _userManager.GetRolesAsync(user);

            // เอา role เดิมออก (ถ้ามี)
            if (currentRoles.Count > 0)
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded) return false;
            }

            // เพิ่ม role ใหม่
            var addResult = await _userManager.AddToRoleAsync(user, newRole);
            if (!addResult.Succeeded)
            {
                // rollback: ใส่ role เดิมกลับ (กันข้อมูลเพี้ยน)
                if (currentRoles.Count > 0)
                {
                    await _userManager.AddToRolesAsync(user, currentRoles);
                }
                return false;
            }

            return true;
        }

    }
}
