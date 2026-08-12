namespace ProjectScientDay2568.Models.Dtos.Competition
{
    public class CompetitionYearDto
    {
        public int CompetitionYearID { get; set; }
        public int YearID { get; set; }
        public int CompetitionID { get; set; }
        public DateTime? CompetitionEndDate { get; set; }
        public string? RegistrationLocation { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsRegistrationOpen { get; set; } = true;
        public int AdvisorCount { get; set; } = 1;
        public string RegistrationMode { get; set; } = "team";
        public int? MaxStudentsPerSchool { get; set; }

        public string? CompetitionName { get; set; } // ชื่อประเภทการเเข่งขัน
        public bool? RequiresProject { get; set; }
    }
}
