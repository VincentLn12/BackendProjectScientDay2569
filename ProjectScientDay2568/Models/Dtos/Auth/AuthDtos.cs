namespace ProjectScientDay2568.Models.Dtos.Auth
{
    public static class AuthDtos
    {
        public record RegisterDto(string Email, string Password);
        public record LoginDto(string Email, string Password);
        public record UserDto(string UserId, string? Email, string[] Roles);
        public record ChangePasswordDto(string CurrentPassword, string NewPassword);
        public record RequestEmailDto(string Email);
        public record ResetPasswordDto(string Email, string Token, string NewPassword);
    }
}
