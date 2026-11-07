namespace ProjectScientDay2568.Models.Dtos.Report
{
    public class CompetitorAnnouncementDto
    {
        public int TeamID { get; set; }
        public string SchoolName { get; set; } = string.Empty;
        public List<string> CompetitorNames { get; set; } = new List<string>();
        public List<string> AdvisorNames { get; set; } = new List<string>();
        public string Remarks { get; set; } = "ผ่านการตรวจสอบ (มีสิทธิ์แข่งขัน)";
        public string? CompetitionLevelName { get; set; }
    }
}
