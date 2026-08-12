namespace ProjectScientDay2568.Models.Dtos.Team
{
    public class TeamCreateDto
    {
        public int? TeamID { get; set; }
        public int? NameOrdinal { get; set; } = 0;
        public int PrimaryTitlePrefix { get; set; }
        public string? PrimaryOtherTitlePrefix { get; set; }
        public string? PrimaryFirstName { get; set; }
        public string? PrimaryLastName { get; set; }
        public int SecondaryTitlePrefix { get; set; }
        public string? SecondaryOtherTitlePrefix { get; set; }
        public string? SecondaryFirstName { get; set; }
        public string? SecondaryLastName { get; set; }
        public int ThirdTitlePrefix { get; set; }
        public string? ThirdOtherTitlePrefix { get; set; }
        public string? ThirdFirstName { get; set; }
        public string? ThirdLastName { get; set; }
        public int FourthTitlePrefix { get; set; }
        public string? FourthOtherTitlePrefix { get; set; }
        public string? FourthFirstName { get; set; }
        public string? FourthLastName { get; set; }
        public int FifthTitlePrefix { get; set; }
        public string? FifthOtherTitlePrefix { get; set; }
        public string? FifthFirstName { get; set; }
        public string? FifthLastName { get; set; }
        public string? UserId { get; set; } // ID ของผู้ใช้ที่สร้างทีม
        public int? CompetitionLevelID { get; set; }
        public string? ProjectName { get; set;  }
        public int? TeamNumberInLevel { get; set; }
        public int? school_code { get; set; } // รหัสโรงเรียน
        public IFormFile? File { get; set; }
    }
}
