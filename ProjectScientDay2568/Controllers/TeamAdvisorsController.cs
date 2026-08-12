using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ProjectScientDay2568.Data;
using ProjectScientDay2568.Models;
using ProjectScientDay2568.Models.Dtos.Team;

namespace ProjectScientDay2568.Controllers
{
    [Authorize]
    [Route("api/teams/{teamId:int}/advisors")]
    [ApiController]
    public class TeamAdvisorsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserContextService _userContext;
        public TeamAdvisorsController(ApplicationDbContext context, UserContextService userContext) { _context = context; _userContext = userContext; }

        [HttpGet]
        public async Task<IActionResult> Get(int teamId)
        {
            var team = await _context.Teams.FindAsync(teamId);
            if (team == null) return NotFound();
            if (team.UserId != _userContext.UserId) return Forbid();
            var result = await _context.TeamAdvisors.Where(x => x.TeamID == teamId).OrderBy(x => x.SortOrder).Select(x => new TeamAdvisorDto { TeamAdvisorID = x.TeamAdvisorID, TeamID = x.TeamID, SortOrder = x.SortOrder, TitlePrefix = (int)x.TitlePrefix, OtherTitlePrefix = x.OtherTitlePrefix, FirstName = x.FirstName, LastName = x.LastName }).ToListAsync();
            return Ok(new ApiResponse { Result = result });
        }

        [HttpPut]
        public async Task<IActionResult> Put(int teamId, [FromBody] List<TeamAdvisorDto> advisors)
        {
            var team = await _context.Teams.FindAsync(teamId);
            if (team == null) return NotFound();
            if (team.UserId != _userContext.UserId) return Forbid();
            var requiredCount = await _context.CompetitionLevels.Where(x => x.CompetitionLevelID == team.CompetitionLevelID).Select(x => x.CompetitionYear!.AdvisorCount).FirstOrDefaultAsync();
            if (advisors.Count != requiredCount || advisors.Any(x => string.IsNullOrWhiteSpace(x.FirstName) || string.IsNullOrWhiteSpace(x.LastName))) return BadRequest("จำนวนหรือข้อมูลผู้ควบคุมทีมไม่ถูกต้อง");
            _context.TeamAdvisors.RemoveRange(_context.TeamAdvisors.Where(x => x.TeamID == teamId));
            _context.TeamAdvisors.AddRange(advisors.Select((x, index) => new TeamAdvisor { TeamID = teamId, SortOrder = index + 1, TitlePrefix = (Utility.SD.TitlePrefix)x.TitlePrefix, OtherTitlePrefix = x.OtherTitlePrefix?.Trim(), FirstName = x.FirstName.Trim(), LastName = x.LastName.Trim() }));
            await _context.SaveChangesAsync(); return NoContent();
        }
    }
}
