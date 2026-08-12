namespace ProjectScientDay2568.Interface
{
    public interface ICompetitionYearService
    {
        Task<List<CompetitionYearDto>> GetCompetitionYearAsync(int YearID);
        Task<CompetitionYearDto> GetCompetitionYearByIdAsync(int id);
        Task<CompetitionYearDto> CreateCompetitionYear(CompetitionYearDto competitionYearDto);
        Task<CompetitionYearDto> UpdateCompetitionYear(int id, CompetitionYearDto competitionYearDto);
        Task DeleteCompetitionYear(int id);

    }
}
