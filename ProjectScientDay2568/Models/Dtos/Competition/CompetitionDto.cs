namespace ProjectScientDay2568.Models.Dtos.Competition
{
    public class CompetitionDto
    {
        public int CompetitionID { get; set; }
        public string CompetitionName { get; set; } = string.Empty;
        public int CompetitionTypeID { get; set; }
        public string? CoordinatorName { get; set; }
        public string? CoordinatorPhone { get; set; }
        public bool RequiresProject { get; set; }
        public string? CompetitionTypeName { get; set; }
    }
}
