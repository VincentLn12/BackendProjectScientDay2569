using ProjectScientDay2568.Models.Dtos.Competition;

namespace ProjectScientDay2568.Interface
{
    public interface ICompetitionTypeService
    {
        Task<List<CompetitionTypeDto>> GetCompetitionTypesAsync();
        Task<CompetitionTypeDto> GetCompetitionTypeByIdAsync(int id);
        Task<CompetitionTypeDto> CreateCompetitionTypeAsync(CompetitionTypeDto competitionType);
        Task<CompetitionTypeDto> UpdateCompetitionTypeAsync(int id, CompetitionTypeDto competitionType);
        Task DeleteCompetitionTypeAsync(int id);
    }
}
