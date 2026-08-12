
using ProjectScientDay2568.Models.Dtos.Team;

namespace ProjectScientDay2568.Models.Dtos.Match
{
    public class MatchDto
    {
        public int Id { get; set; }
        public int CompetitionLevelID { get; set; }
        public int Round { get; set; }
        public TeamDto? TeamA { get; set; }
        public TeamDto? TeamB { get; set; }
        public TeamDto? Winner { get; set; }
        public string Status { get; set; } 
        public DateTime? MatchTime { get; set; } = DateTime.UtcNow;
        public string? school_name { get; set; }

    }
}
