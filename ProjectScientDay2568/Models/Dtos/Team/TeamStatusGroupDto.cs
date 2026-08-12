namespace ProjectScientDay2568.Models.Dtos.Team
{
    public class TeamStatusGroupDto
    {
        public List<TeamDto> CheckedInTeams { get; set; } = new();
        public List<TeamDto> VerifiedTeams { get; set; } = new();
    }
}
