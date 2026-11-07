namespace ProjectScientDay2568.Models
{
    // บันทึกการกระทำสำคัญ (ใครทำอะไร กับอะไร เมื่อไร) อ่านอย่างเดียว ไม่มี API แก้/ลบ
    // ห้ามเก็บรหัสผ่าน token หรือข้อมูลส่วนตัวของนักเรียนใน Details
    public class AuditLog
    {
        [Key] public long Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [MaxLength(256)] public string? Actor { get; set; }
        [Required, MaxLength(100)] public string Action { get; set; } = string.Empty;
        [MaxLength(256)] public string? Target { get; set; }
        [MaxLength(2000)] public string? Details { get; set; }
        [MaxLength(64)] public string? IpAddress { get; set; }
    }
}
