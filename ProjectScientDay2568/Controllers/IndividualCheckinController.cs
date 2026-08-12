using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ProjectScientDay2568.Data;
using ProjectScientDay2568.Models.Dtos.Team;

namespace ProjectScientDay2568.Controllers
{
    [Authorize(Roles = "Admin,admin,Staff,staff,เจ้าหน้าที่")]
    [Route("api/individual-checkin")]
    [ApiController]
    public class IndividualCheckinController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public IndividualCheckinController(ApplicationDbContext context) => _context = context;
        [HttpGet("competition-level/{competitionLevelId:int}")]
        public async Task<IActionResult> Get(int competitionLevelId) => Ok(new ApiResponse { Result = await _context.Students.Where(s => s.Team!.CompetitionLevelID == competitionLevelId).Select(s => new StudentDto { StudentID = s.StudentID, TeamID = s.TeamID, TitlePrefix = (int)s.TitlePrefix, FirstName = s.FirstName, LastName = s.LastName, IsMainMember = s.IsMainMember, RewardTypeID = s.RewardTypeID, IsCertificateGenerated = s.IsCertificateGenerated, SchoolName = s.Team!.schools.school_name }).ToListAsync() });
        [HttpPut("{studentId:int}/reward")]
        public async Task<IActionResult> UpdateReward(int studentId, [FromBody] int? rewardTypeId) { var student = await _context.Students.FindAsync(studentId); if (student == null) return NotFound(); student.RewardTypeID = rewardTypeId; await _context.SaveChangesAsync(); return NoContent(); }
        [HttpPut("{studentId:int}/check-in")]
        public async Task<IActionResult> CheckIn(int studentId) { var student = await _context.Students.FindAsync(studentId); if (student == null) return NotFound(); student.IsCheckedIn = true; await _context.SaveChangesAsync(); return NoContent(); }
    }
}
