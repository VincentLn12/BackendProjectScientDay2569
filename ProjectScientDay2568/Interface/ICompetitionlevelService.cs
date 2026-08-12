namespace ProjectScientDay2568.Interface
{
    public interface ICompetitionlevelService
    {
        Task<List<CompetitionLevelDto>> GetCompetitionLevelListAsync(int CompetitionYearID);
        Task<CompetitionLevelDto> GetCompetitionLevelByIdAsync(int id);
        Task<CompetitionLevelDto> CreateCompetitionLevel(CompetitionLevelDto competitionLevelDto);
        Task<CompetitionLevelDto> UpdateCompetitionLevel(int id, CompetitionLevelDto competitionLevelDto);
        Task DeleteCompetitionLevel(int id);
    }
}
