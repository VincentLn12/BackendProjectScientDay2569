using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectScientDay2568.Models
{
        public class CertificateConfig
        {
            [Key]
            [ForeignKey(nameof(Year))]
            public int YearID { get; set; } // อ้างอิงไปยังปีการแข่งขัน
            public int StartNumber { get; set; } // เลขรันเริ่มต้น เช่น 221
            public string CertificateName { get; set; } = string.Empty; // เช่น "กจ"
            //background path สำหรับใบประกาศนียบัตร
            public string? CertificateBackgroundPath { get; set; } // << ย้ายมาที่นี่

            // คณบดีมหาวิทยาลัย / ผู้ลงนามที่ 1
            public string? UniversityDeanTitle { get; set; } = "อธิการบดีมหาวิทยาลัย";
            public string? UniversityDeanName { get; set; } = string.Empty;
            public string? UniversityDeanSignature { get; set; } = string.Empty;
            // คณบดีคณะ / ผู้ลงนามที่ 2
            public string? FacultyDeanTitle { get; set; } = "คณบดีคณะวิทยาศาสตร์และเทคโนโลยี";
            public string? FacultyDeanName { get; set; } = string.Empty;
            public string? FacultyDeanSignature { get; set; } = string.Empty;

            //โลโก้มหาวิทยาลัย
            public string? UniversityLogoPath { get; set; } = string.Empty;
            //โลโก้สถาบันวิทยาศาสตร์
            public string? InstituteLogoPath { get; set; } = string.Empty;

            public Year Year { get; set; }
        }

}
