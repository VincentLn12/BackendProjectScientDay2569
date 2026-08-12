
namespace ProjectScientDay2568.Models
{
    public class Year
    {
        [Key]
        public int YearID { get; set; }
        [Required]
        public int YearNumber { get; set; }
        [Required]
        [MaxLength(255)]
        public string YearName { get; set; } = string.Empty;
        public bool isActive { get; set; } = false;
        public string? CompetitionRegulationsUrl { get; set; } = string.Empty;
        //public string? CertificateBackgroundPath { get; set; }
        public string? DateName { get; set; } = string.Empty;
        public DateTime? RegistrationOpenDate { get; set; }
        public DateTime? RegistrationCloseDate { get; set; }
        public ICollection<CompetitionYear>? CompetitionYears { get; set; }
        public CertificateConfig CertificateConfig { get; set; }
    }
}
