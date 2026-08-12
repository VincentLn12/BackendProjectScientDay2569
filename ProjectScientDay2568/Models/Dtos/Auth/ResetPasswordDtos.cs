namespace ProjectScientDay2568.Models.Dtos.Auth
{
    public class ResetPasswordDtos
    {
        public class GenerateResetLinkRequest
        {
            public string? UserName { get; set; }
            public string? UserId { get; set; }
        }

        public class GenerateResetLinkResponse
        {
            public string UserId { get; set; } = default!;
            public string ResetUrl { get; set; } = default!;
            public DateTimeOffset? ExpiresAt { get; set; } // optional: แจ้งหมดอายุ
        }

        // ใช้ตอนผู้ใช้ submit รหัสใหม่
        public class ResetPasswordRequest
        {
            [Required] public string UserId { get; set; } = default!;
            [Required] public string Token { get; set; } = default!; // base64url-encoded token
            [Required] public string NewPassword { get; set; } = default!;
        }
        public class ResetLinkRequest
        {
            public string Email { get; set; }
            public string ClientBaseUrl { get; set; } // เช่น https://portal.univ.ac.th
        }
    }
}
