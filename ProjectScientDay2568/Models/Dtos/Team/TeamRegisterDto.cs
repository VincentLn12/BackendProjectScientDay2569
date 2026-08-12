using static ProjectScientDay2568.Utility.SD;

namespace ProjectScientDay2568.Models.Dtos.Team
{
    public class TeamRegisterDto
    {
        public int TeamID { get; set; }
        public int? CompetitionLevelID { get; set; }
        public string? UserId { get; set; }
        public TeamStatus? Status { get; set; }
        public string? ProjectPDFLink { get; set; } // ลิงก์ PDF ที่อัปโหลดแล้ว
        public DateTime? StatusTime { get; set; } 
        public bool? RequiresProject { get; set; }
        public int? SchoolCode { get; set; } 
        public string? SchoolName { get; set; } // ชื่อโรงเรียน
    }
}
