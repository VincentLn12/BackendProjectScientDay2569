namespace ProjectScientDay2568.Interface
{
    public interface ICompetitionTypeYearService    
    {
        Task<List<CompetitionTypeYearDto>> GetCompetitionTypesYear();
       
    }
}
