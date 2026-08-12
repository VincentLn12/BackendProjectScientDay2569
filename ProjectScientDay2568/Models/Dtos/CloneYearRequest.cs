namespace ProjectScientDay2568.Models.Dtos
{
    public class CloneYearRequest
    {
        public int SourceYearId { get; set; }
        public int NewYearNumber { get; set; }
        public string NewYearName { get; set; } = string.Empty;

        // ตัวเลือกการคัดลอก (เริ่มที่ false ให้ปลอดภัย)
        public bool CopyCertificateBackgroundFile { get; set; } = true;
        public bool CopyCertificateConfig { get; set; } = true;
        public bool ResetCertificateStartNumberToOne { get; set; } = true;

        public bool CopyCompetitionYears { get; set; } = true;
        public bool CopyCompetitionLevels { get; set; } = true;
        public bool CopyAwards { get; set; } = true;

        // ถ้าต้องการก็อปทีม/แมตช์ ค่อยเปิด (ตัวอย่างโค้ดเผื่อไว้)
        public bool CopyTeams { get; set; } = false;
        public bool CopyMatches { get; set; } = false;
    }
}
