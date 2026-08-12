
namespace ProjectScientDay2568.Interface
{
    public interface ICompetitionService
    {
        Task<List<CompetitionDto>> GetCompetitionListAsync();
        Task<CompetitionDto> GetCompetitionByIdAsync(int id);
        Task<CompetitionDto> CreateCompetition(CompetitionDto competitionTypeDto);
        Task<CompetitionDto> UpdateCompetition(int id ,CompetitionDto competitionTypeDto);
        Task DeleteCompetition(int id);
        
    }
}
