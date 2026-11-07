
using ProjectScientDay2568.Models.Dtos.Team;

namespace ProjectScientDay2568.Interface
{
    public interface ITeamService
    {
        Task<List<TeamDto>> GetAllTeamAsync();
        //Task<TeamCreateDto> CreateTeamAsync(TeamCreateDto teamDto , string userID);
        Task<TeamCreateDto> UpdateTeamAsync(int id, TeamCreateDto teamDto, string userID);
        Task DeleteTeamAsync(int id);
        Task ChangeTeamLevelAsync(ChangeTeamLevelDto dto);
    }
}
