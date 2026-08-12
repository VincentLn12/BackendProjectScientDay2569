namespace ProjectScientDay2568.Models.Dtos.Team
{
    public class CertificateConfigDto
    {
        public int YearID { get; set; } // อ้างอิงไปยังปีการแข่งขัน
        public int StartNumber { get; set; } // เลขรันเริ่มต้น เช่น 221
        public string CertificateName { get; set; } = string.Empty; // เช่น "กจ"
        public string? UniversityDeanTitle { get; set; } = string.Empty;
        public string? UniversityDeanName { get; set; } = string.Empty; 
        public string? UniversityDeanSignature { get; set; } = string.Empty;
        public string? FacultyDeanTitle { get; set; } = string.Empty;
        public string? FacultyDeanName { get; set; } = string.Empty;
        public string? FacultyDeanSignature { get; set; } = string.Empty;
        public string? CertificateBackgroundPath { get; set; } // << ย้ายมาที่นี่
        public string? UniversityLogoPath { get; set; } = string.Empty;
        public string? InstituteLogoPath { get; set; } = string.Empty;
    }
}
