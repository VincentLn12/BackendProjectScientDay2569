namespace ProjectScientDay2568.Models.Dtos.Team
{
    public class TeamCopyRequest
    {
        public int SourceTeamId { get; set; }
        public int? TargetCompetitionLevelID { get; set; }
        public bool IncludeStudents { get; set; } = true;
        public string? OverrideProjectName { get; set; }
        public bool ResetStatusToCheckedIn { get; set; } = true;
    }
    public class TeamCopyResultDto
    {
        public int NewTeamId { get; set; }
        public int? CompetitionLevelID { get; set; }
        public int? TeamNumberInLevel { get; set; }
        public int? NameOrdinal { get; set; }
    }
}
