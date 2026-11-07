namespace ProjectScientDay2568.Helper
{
    // LockoutEnd ของ Identity ใช้ 2 แบบ:
    // - ระงับโดยแอดมิน: ตั้งเป็น DateTimeOffset.MaxValue (ไม่มีกำหนด)
    // - ล็อกชั่วคราวเพราะใส่รหัสผิดหลายครั้ง: แค่ไม่กี่นาที
    // แยกกันด้วยเกณฑ์ "เกิน 10 ปีข้างหน้า" = ระงับ
    public static class AccountSuspension
    {
        public static DateTimeOffset Threshold => DateTimeOffset.UtcNow.AddYears(10);

        public static bool IsSuspended(ApplicationUser user) =>
            user.LockoutEnd != null && user.LockoutEnd > Threshold;
    }
}
