using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectScientDay2568.Models
{
    public class TeamCertificate
    {
        [Key]
        public int CertificateID { get; set; }

        public int TeamID { get; set; }
        [ForeignKey("TeamID")]
        public Team Team { get; set; }

        [Required]
        [MaxLength(255)]
        public string? CertificateNumber { get; set; } // เช่น กจ.331/2568
        public string? CertificateName { get; set; } // เช่น กจ.331/2568

        public DateTime IssuedDate { get; set; } = DateTime.Now;
    }
}
