using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectScientDay2568.Models
{
    public class MatchDrawLog
    {
        [Key]
        public int Id { get; set; }

        public int CompetitionLevelID { get; set; }

        [ForeignKey("CompetitionLevelID")]
        public CompetitionLevel? CompetitionLevel { get; set; }

        public int Round { get; set; }

        public string ActionType { get; set; } = "GENERATE_FIRST_ROUND";

        public string Note { get; set; } = string.Empty;

        public string MatchesJson { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.AddHours(7);
    }
}