namespace ProjectScientDay2568.Models.Dtos.Competition
{
    public class CompetitionLevelDto
    { 
        public int CompetitionLevelID { get; set; }
        public int CompetitionYearID { get; set; }
        public int LevelID { get; set; }
        public int? MinTeamMembers { get; set; }
        public int? MaxTeamMembers { get; set; }
        public int? MainPlayersCount { get; set; }
        public int? ReservePlayersCount { get; set; }
        public int AdvisorCount { get; set; } = 1;
        public string RegistrationMode { get; set; } = "team";
        public string? LevelName { get; set; }
        public DateTime CompetitionDateTime { get; set; } = DateTime.Now;
        public bool IsCertificateNumberGenerated { get; set; } 


    }
}
