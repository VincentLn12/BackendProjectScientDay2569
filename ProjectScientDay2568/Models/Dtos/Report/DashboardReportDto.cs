namespace ProjectScientDay2568.Models.Dtos.Report
{
    public class DashboardReportDto
    {
        public int TotalSchools { get; set; }
        public int TotalTeams { get; set; }
        public int TotalStudents { get; set; }
        public int TotalCompetitions { get; set; }
        public int TotalRegisteredStudents { get; set; }
        public int TotalCheckedInStudents { get; set; }

        public int RegisteredTeams { get; set; }
        public int VerifiedTeams { get; set; }
        public int ProjectRevisionTeams { get; set; }
        public int CheckedInTeams { get; set; }
        public int DisqualifiedTeams { get; set; }

        public int TotalProjects { get; set; }
        public int TotalCertificates { get; set; }

        public List<TeamStatusSummaryDto> TeamStatusSummary { get; set; } = new();
        public List<CompetitionSummaryDto> CompetitionSummary { get; set; } = new();
        public List<CompetitionAttendanceSummaryDto> CompetitionAttendanceSummary { get; set; } = new();
        public List<EducationLevelAttendanceSummaryDto> EducationLevelAttendanceSummary { get; set; } = new();
        public List<SchoolSummaryDto> SchoolSummary { get; set; } = new();
        public List<RewardSummaryDto> RewardSummary { get; set; } = new();
    }

    public class TeamStatusSummaryDto
    {
        public string StatusName { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class CompetitionSummaryDto
    {
        public string CompetitionName { get; set; } = string.Empty;
        public string? LevelName { get; set; }
        public int TeamCount { get; set; }
        public int StudentCount { get; set; }
        public int SchoolCount { get; set; }
    }

    public class CompetitionAttendanceSummaryDto
    {
        public int CompetitionYearID { get; set; }
        public string CompetitionName { get; set; } = string.Empty;
        public int RegisteredTeamCount { get; set; }
        public int RegisteredStudentCount { get; set; }
        public int CheckedInTeamCount { get; set; }
        public int CheckedInStudentCount { get; set; }
        public List<EducationLevelAttendanceSummaryDto> Levels { get; set; } = new();
    }

    public class EducationLevelAttendanceSummaryDto
    {
        public int LevelID { get; set; }
        public string LevelName { get; set; } = string.Empty;
        public int RegisteredTeamCount { get; set; }
        public int RegisteredStudentCount { get; set; }
        public int CheckedInTeamCount { get; set; }
        public int CheckedInStudentCount { get; set; }
    }

    public class SchoolSummaryDto
    {
        public string SchoolName { get; set; } = string.Empty;
        public string? Province { get; set; }
        public int TeamCount { get; set; }
        public int StudentCount { get; set; }
    }

    public class RewardSummaryDto
    {
        public string RewardName { get; set; } = string.Empty;
        public int TeamCount { get; set; }
    }
}
