namespace ProjectScientDay2568.Models.Dtos.Team
{
    public class TeamProjectCreateDto
    {
        public int TeamID { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public IFormFile? File { get; set; }
    }
    public class TeamProjectUpdateDto
    {
        public string ProjectName { get; set; } = string.Empty;
        public IFormFile? File { get; set; }
    }
}
