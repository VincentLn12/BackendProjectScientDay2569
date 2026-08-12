namespace ProjectScientDay2568.Models.Dtos.Report
{
    public class PublicReportYearDto
    {
        public int YearID { get; set; }
        public int YearNumber { get; set; }
        public string YearName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class PublicReportCompetitionYearDto
    {
        public int CompetitionYearID { get; set; }
        public int YearID { get; set; }
        public int CompetitionID { get; set; }
        public string CompetitionName { get; set; } = string.Empty;
        public DateTime? CompetitionEndDate { get; set; }
        public string? RegistrationLocation { get; set; }
        public bool RequiresProject { get; set; }
        public int SchoolCount { get; set; }
        public int StudentCount { get; set; }
    }

    public class PublicReportCompetitionLevelDto
    {
        public int CompetitionLevelID { get; set; }
        public int CompetitionYearID { get; set; }
        public int LevelID { get; set; }
        public string LevelName { get; set; } = string.Empty;
        public int SchoolCount { get; set; }
        public int StudentCount { get; set; }
        public DateTime? CompetitionDateTime { get; set; }
    }

    public class PublicReportCompetitionLevelSummaryDto
    {
        public int CompetitionLevelID { get; set; }
        public int CompetitionYearID { get; set; }
        public int YearID { get; set; }
        public string YearName { get; set; } = string.Empty;
        public int YearNumber { get; set; }
        public string CompetitionName { get; set; } = string.Empty;
        public string LevelName { get; set; } = string.Empty;
        public bool RequiresProject { get; set; }
        public string? RegistrationLocation { get; set; }
        public DateTime? CompetitionDateTime { get; set; }
        public int SchoolCount { get; set; }
        public int TeamCount { get; set; }
        public int StudentCount { get; set; }
    }

    public class PublicReportTeamDto
    {
        public int TeamID { get; set; }
        public string SchoolName { get; set; } = string.Empty;
        public string? Province { get; set; }
        public string? District { get; set; }
        public string? TeamName { get; set; }
        public string? ProjectName { get; set; }
        public string? ProjectPDFLink { get; set; }
        public string? TeacherName { get; set; }
        public int StudentCount { get; set; }
        public int MainStudentCount { get; set; }
        public int ReserveStudentCount { get; set; }
    }

    public class PublicReportStudentDto
    {
        public int StudentID { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? TitlePrefixLabel { get; set; }
        public bool IsMainMember { get; set; }
    }

    public class PublicReportPagedTeamsDto
    {
        public List<PublicReportTeamDto> Items { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
