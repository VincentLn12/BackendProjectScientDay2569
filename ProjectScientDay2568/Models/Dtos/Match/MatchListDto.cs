namespace ProjectScientDay2568.Models.Dtos.Match
{
    /// <summary>
    /// Lightweight payload for match lists. It intentionally contains only the
    /// data required to display a pairing and submit a winner.
    /// </summary>
    public class MatchListDto
    {
        public int Id { get; set; }
        public int CompetitionLevelID { get; set; }
        public int Round { get; set; }
        public MatchTeamDto? TeamA { get; set; }
        public MatchTeamDto? TeamB { get; set; }
        public MatchTeamDto? Winner { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? MatchTime { get; set; }
    }

    public class MatchTeamDto
    {
        public int TeamID { get; set; }
        public int? TeamNumberInLevel { get; set; }
        public string? school_name { get; set; }
    }
}
