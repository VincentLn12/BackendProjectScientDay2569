namespace ProjectScientDay2568.Models.Dtos.Auth
{
    public class UserRoleViewModel
    {
        public string Id { get; set; } = default!;    // <-- เพิ่ม
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string CurrentRoles { get; set; } = null!;
        public string? SelectedRole { get; set; }
    }
}
