using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectScientDay2568.Models
{
    public class Match
    {
        [Key]
        public int Id { get; set; }
        public int CompetitionLevelID { get; set; }
        [ForeignKey("CompetitionLevelID")]
        public CompetitionLevel? CompetitionLevel { get; set; }
        public int Round { get; set; }
        public Team? TeamA { get; set; }
        public Team? TeamB { get; set; }
        public Team? Winner { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime? MatchTime { get; set; } = DateTime.UtcNow;

    }
}
