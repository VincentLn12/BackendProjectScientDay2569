namespace ProjectScientDay2568.Models.Dtos.Team
{
    public class StudentDto
    {
        public int StudentID { get; set; }
        public int TitlePrefix { get; set; } 
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public int TeamID { get; set; }
        public bool IsMainMember { get; set; } = true;
        public int? RewardTypeID { get; set; }
        public string? RewardTypeName { get; set; }
        public bool IsCertificateGenerated { get; set; }
        public bool IsCheckedIn { get; set; }
        public string? SchoolName { get; set; }
    }
}
