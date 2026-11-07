using System.ComponentModel.DataAnnotations;

namespace ProjectScientDay2568.Models
{
    public class SystemSetting
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string ProjectName { get; set; } = "คณะวิทยาศาสตร์และเทคโนโลยี";

        [MaxLength(255)]
        public string ProjectSubtitle { get; set; } = "มหาวิทยาลัยราชภัฏกาญจนบุรี";

        public bool ShowNavbar { get; set; } = true;

        [MaxLength(500)]
        public string LogoPath { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Theme { get; set; } = "emerald";

        public bool ShowTrainingMenu { get; set; } = true;

        public bool ShowNewsMenu { get; set; } = true;

        public bool ShowResultsMenu { get; set; } = true;

        public bool ShowActivityMenu { get; set; } = true;

        /// <summary>
        /// JSON configuration for editable homepage copy and visibility settings.
        /// Kept as JSON so new homepage blocks can be added without a schema change.
        /// </summary>
        public string? HomepageContent { get; set; }

        [MaxLength(500)] public string? HomeHeroTitle { get; set; }
        [MaxLength(500)] public string? HomeHeroHighlight { get; set; }
        [MaxLength(2000)] public string? HomeHeroDescription { get; set; }
        [MaxLength(100)] public string? HomeRegisterButtonLabel { get; set; }
        [MaxLength(100)] public string? HomeGuideButtonLabel { get; set; }
        [MaxLength(100)] public string? HomeApplicantsButtonLabel { get; set; }
        public bool ShowHomeNews { get; set; } = true;
        [MaxLength(500)] public string? HomeNewsTitle { get; set; }
        [MaxLength(1000)] public string? HomeNewsDescription { get; set; }
        [MaxLength(100)] public string? HomeNewsButtonLabel { get; set; }
        public int? HomeDisplayYearId { get; set; }

        // คู่มือแอดมิน: ไฟล์อยู่ใน wwwroot/uploads/settings (เปิดได้จากลิงก์ไฟล์โดยตรง)
        [MaxLength(500)] public string? AdminManualPath { get; set; }
        [MaxLength(255)] public string? AdminManualFileName { get; set; }
        public DateTime? AdminManualUpdatedAt { get; set; }

        // คู่มือผู้ใช้ (สาธารณะ): ไฟล์อยู่ใน wwwroot/uploads/settings แสดงบนแถบเมนู ท้ายเว็บ และหน้าเลือกปี
        [MaxLength(500)] public string? UserManualPath { get; set; }
        [MaxLength(255)] public string? UserManualFileName { get; set; }
        public DateTime? UserManualUpdatedAt { get; set; }

    }
}
