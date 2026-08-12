namespace ProjectScientDay2568.Models
{
    public class TeamProject
    {
        public int TeamProjectID { get; set; }

        public int TeamID { get; set; }
        public Team Team { get; set; } = null!;

        public string ProjectName { get; set; } = string.Empty;
        public string ProjectPDFLink { get; set; } = string.Empty;
    }
}
