using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectScientDay2568.Models
{
    public class CompetitionLevel
    {
        [Key]
        public int CompetitionLevelID { get; set; }

        [ForeignKey("CompetitionYear")]
        public int CompetitionYearID { get; set; }
        public CompetitionYear? CompetitionYear { get; set; }

        [ForeignKey("EducationLevel")]
        public int LevelID { get; set; }
        public EducationLevel? EducationLevel { get; set; }
        public DateTime CompetitionDateTime { get; set; }       
        public int? MainPlayersCount { get; set; }
        public int? ReservePlayersCount { get; set; }

        public bool IsCertificateNumberGenerated { get; set; } = false;

        // Navigation properties
        public ICollection<Team>? Teams { get; set; }
        public ICollection<Match>? Matches { get; set; }

    }
}
