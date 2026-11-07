using System;

namespace ProjectScientDay2568.Models.Dtos.Match
{
    public class MatchDrawLogDto
    {
        public int Id { get; set; }
        public int CompetitionLevelID { get; set; }
        public int Round { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public string MatchesJson { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}