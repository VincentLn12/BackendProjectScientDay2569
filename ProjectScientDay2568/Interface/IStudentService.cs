
using ProjectScientDay2568.Models.Dtos.Team;

namespace ProjectScientDay2568.Interface
{
    public interface IStudentService
    {
        Task<List<StudentDto>> GetAllStudentAsync(int teamId);
        Task<StudentDto> CreateStudentAsync(StudentDto studentDto);
        Task<StudentDto> UpdateStudentAsync(int id, StudentDto studentDto);
        Task DeleteStudentAsync(int id);
    }
}
