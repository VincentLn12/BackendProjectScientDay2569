using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectScientDay2568.Models
{
    public class Trainingtopic
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime? TrainingDate { get; set; }

        public TimeSpan? StartTime { get; set; }

        public TimeSpan? EndTime { get; set; }

        public int MaxParticipants { get; set; } = 0;

        public bool IsOpenRegister { get; set; } = true;

        public DateTime? RegisterStartDate { get; set; }

        public DateTime? RegisterEndDate { get; set; }

        public string? Location { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? TeacherName { get; set; }

        public string? Trainingtopicsrequirements { get; set; }

        // ผู้เข้าร่วมกิจกรรม เช่น "นักเรียนชั้นมัธยมศึกษาปีที่ 4-6"
        [MaxLength(500)]
        public string? Audience { get; set; }

        public int? Branch_id { get; set; }

        [ForeignKey("Branch_id")]
        public Branch? branch { get; set; }

        public int? CertificateTemplateId { get; set; }

        [ForeignKey("CertificateTemplateId")]
        public CertificateConfig? CertificateTemplate { get; set; }

        // ปีการจัดงาน (ตาราง Years เดียวกับการแข่งขัน)
        public int? YearID { get; set; }

        [ForeignKey("YearID")]
        public Year? Year { get; set; }

        // กลุ่มไลน์ของหลักสูตร (ไม่บังคับ) ส่งให้เฉพาะผู้ที่สมัครแล้วและเจ้าหน้าที่
        [MaxLength(500)] public string? LineGroupUrl { get; set; }
        [MaxLength(500)] public string? LineQrPath { get; set; }
        [MaxLength(500)] public string? LineNote { get; set; }

        [NotMapped]
        public bool HasLineGroup { get; set; }

        // ใช้กับ entity ที่ไม่ได้บันทึกต่อเท่านั้น (ก่อนตอบกลับ)
        public void HideLineGroup()
        {
            HasLineGroup = !string.IsNullOrEmpty(LineGroupUrl) || !string.IsNullOrEmpty(LineQrPath);
            LineGroupUrl = null;
            LineQrPath = null;
            LineNote = null;
        }

        [NotMapped]
        public int RegisteredCount { get; set; }
    }
}
