using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectScientDay2568.Models
{
    public class Competition
    {
        [Key]
        public int CompetitionID { get; set; }

        [Required]
        [MaxLength(255)]
        public string CompetitionName { get; set; } = string.Empty;

        [ForeignKey("CompetitionType")]
        public int CompetitionTypeID { get; set; }
        public CompetitionType? CompetitionType { get; set; }

        [MaxLength(255)]
        public string? CoordinatorName { get; set; } 

        [MaxLength(50)]
        public string? CoordinatorPhone { get; set; }
        public bool RequiresProject { get; set; }

        // Navigation properties
        public ICollection<CompetitionYear>? CompetitionYears { get; set; }
    }
}
