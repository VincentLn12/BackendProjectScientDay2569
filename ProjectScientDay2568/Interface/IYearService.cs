namespace ProjectScientDay2568.Interface
{
    public interface IYearService
    {
        Task<List<YearDto>> GetAllYearsAsync();
        Task<YearDto> GetYearByIdAsync(int id);
        Task<YearDto> CreateYearAsync(YearDto yearDto);
        Task<YearDto> UpdateYearAsync(int id, YearDto yearDto);
        Task DeleteYearAsync(int id);
    }
}
