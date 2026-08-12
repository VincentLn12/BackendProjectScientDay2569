namespace ProjectScientDay2568.Models.Dtos.Auth
{
    public class RoleUpdateDto : RoleDto
    {
        public string Id { get; set; } = default!;
        public string UpdateName { get; set; } = string.Empty;
    }
}
