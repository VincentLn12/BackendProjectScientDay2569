using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProjectScientDay2568.Models.Dtos.Auth;

namespace ProjectScientDay2568.Service
{
    public class RoleService
    {
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly UserManager<ApplicationUser> userManager;

        public RoleService(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager)
        {
            this.roleManager = roleManager;
            this.userManager = userManager;
        }

        public async Task<bool> Add(RoleDto roleDto)
        {
            if (string.IsNullOrWhiteSpace(roleDto?.Name)) return false;

            // กันชื่อซ้ำ
            if (await roleManager.RoleExistsAsync(roleDto.Name)) return false;

            var identityRole = new IdentityRole
            {
                Name = roleDto.Name,
                NormalizedName = roleManager.NormalizeKey(roleDto.Name)
            };

            var result = await roleManager.CreateAsync(identityRole);
            return result.Succeeded;
        }

        public async Task<bool> Delete(string id)
        {
            var identityRole = await Find(id);
            if (identityRole == null) return false;

            // ถ้ามี user ถือ role อยู่ ห้ามลบ
            var usersInRole = await userManager.GetUsersInRoleAsync(identityRole.Name!);
            if (usersInRole.Count != 0) return false;

            var result = await roleManager.DeleteAsync(identityRole);
            return result.Succeeded;
        }

        public async Task<IdentityRole?> Find(string id)
        {
            return await roleManager.FindByIdAsync(id);
        }

        public async Task<List<IdentityRole>> GetAll()
        {
            return await roleManager.Roles.ToListAsync();
        }

        public async Task<bool> Update(RoleUpdateDto roleUpdateDto)
        {
            if (roleUpdateDto == null ||
                string.IsNullOrWhiteSpace(roleUpdateDto.Id) ||
                string.IsNullOrWhiteSpace(roleUpdateDto.UpdateName))
            {
                return false;
            }

            var identityRole = await Find(roleUpdateDto.Id);
            if (identityRole == null) return false;

            // กันชื่อซ้ำ
            if (!identityRole.Name!.Equals(roleUpdateDto.UpdateName, StringComparison.OrdinalIgnoreCase)
                && await roleManager.RoleExistsAsync(roleUpdateDto.UpdateName))
            {
                return false;
            }

            identityRole.Name = roleUpdateDto.UpdateName;
            identityRole.NormalizedName = roleManager.NormalizeKey(roleUpdateDto.UpdateName);

            var result = await roleManager.UpdateAsync(identityRole);
            return result.Succeeded;
        }
    }
}
