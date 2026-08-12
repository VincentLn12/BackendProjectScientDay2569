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

        public int? Branch_id { get; set; }

        [ForeignKey("Branch_id")]
        public Branch? branch { get; set; }

        public int? CertificateTemplateId { get; set; }

        [ForeignKey("CertificateTemplateId")]
        public CertificateConfig? CertificateTemplate { get; set; }

        [NotMapped]
        public int RegisteredCount { get; set; }
    }
}
