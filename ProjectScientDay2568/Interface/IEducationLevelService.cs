namespace ProjectScientDay2568.Interface
{
    public interface IEducationLevelService
    {
        Task<List<EducationLevelDto>> GetEducationLevels();
        Task<EducationLevelDto> GetEducationLevelsId(int id);
        Task<EducationLevelDto> CreateEducationLevelsAsync(EducationLevelDto educationLevelDto);
        Task<EducationLevelDto> UpdateEducationLevelsAsync(int id,EducationLevelDto educationLevelDto);
        Task DeleteEducationLevelAsync(int id);
    }
}
