using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectScientDay2568.Models
{
    public class TrainingRegistration
    {
        [Key]
        public int Id { get; set; }

        public int TrainingtopicId { get; set; }

        [ForeignKey("TrainingtopicId")]
        public Trainingtopic? Trainingtopic { get; set; }

        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        public string? Prefix { get; set; }

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

        public string Status { get; set; } = "ลงทะเบียนแล้ว";
    }
}
