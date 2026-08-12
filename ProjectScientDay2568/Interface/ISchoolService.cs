using ProjectScientDay2568.Models.Dtos.Team;

namespace ProjectScientDay2568.Interface
{
    public interface ISchoolService
    {
        Task<List<SchoolDto>> GetSchool();
        Task<SchoolDto> CreateSchoolAsync(SchoolDto schoolDto);
        Task<SchoolDto> UpdateSchooAsync(int schoolCode, SchoolDto schoolDto);
        Task DeleteSchooAsync(int id);
    }
}
