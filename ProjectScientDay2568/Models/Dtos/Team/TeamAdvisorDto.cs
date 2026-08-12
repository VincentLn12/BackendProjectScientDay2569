namespace ProjectScientDay2568.Models.Dtos.Team
{
    public class TeamAdvisorDto
    {
        public int? TeamAdvisorID { get; set; }
        public int TeamID { get; set; }
        public int SortOrder { get; set; }
        public int TitlePrefix { get; set; }
        public string? OtherTitlePrefix { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }
}
