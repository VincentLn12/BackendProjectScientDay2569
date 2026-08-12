using ProjectScientDay2568.Models.Dtos.Report;
using ProjectScientDay2568.Utility;

namespace ProjectScientDay2568.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private static readonly SD.TeamStatus[] DashboardStatuses =
        [
            SD.TeamStatus.Registered,
            SD.TeamStatus.Verified,
            SD.TeamStatus.ProjectRevision,
            SD.TeamStatus.CheckedIn,
            SD.TeamStatus.Disqualified
        ];

        private readonly ApplicationDbContext _context;

        public ReportController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult<DashboardReportDto>> GetDashboardReport([FromQuery] int? yearId)
        {
            var compYearsQuery = _context.CompetitionYears
                .AsNoTracking()
                .Include(cy => cy.Competition)
                .Where(cy => cy.IsActive);

            if (yearId.HasValue && yearId.Value > 0)
            {
                compYearsQuery = compYearsQuery.Where(cy => cy.YearID == yearId.Value);
            }

            var compYears = await compYearsQuery.ToListAsync();
            var compYearIds = compYears.Select(cy => cy.CompetitionYearID).ToList();

            var compLevels = await _context.CompetitionLevels
                .AsNoTracking()
                .Include(cl => cl.EducationLevel)
                .Where(cl => compYearIds.Contains(cl.CompetitionYearID))
                .ToListAsync();

            var teamsQuery = _context.Teams
                .AsNoTracking()
                .Include(t => t.Students)
                .Include(t => t.TeamProjects)
                .Include(t => t.schools)
                .Include(t => t.RewardType)
                .Where(t => t.CompetitionLevelID.HasValue && compLevels.Select(cl => cl.CompetitionLevelID).Contains(t.CompetitionLevelID.Value))
                .AsQueryable();

            var teams = await teamsQuery.ToListAsync();
            var teamIds = teams.Select(t => t.TeamID).ToList();

            var teamStatusSummary = DashboardStatuses
                .Select(status => new TeamStatusSummaryDto
                {
                    StatusName = GetTeamStatusName(status),
                    Count = teams.Count(t => t.Status == status)
                })
                .ToList();

            var competitionSummary = compLevels
                .Select(cl =>
                {
                    var levelTeams = teams.Where(t => t.CompetitionLevelID == cl.CompetitionLevelID).ToList();
                    var compYear = compYears.FirstOrDefault(cy => cy.CompetitionYearID == cl.CompetitionYearID);
                    return new CompetitionSummaryDto
                    {
                        CompetitionName = compYear?.Competition?.CompetitionName ?? "-",
                        LevelName = cl.EducationLevel?.LevelName ?? "-",
                        TeamCount = levelTeams.Count,
                        StudentCount = levelTeams.Sum(t => t.Students?.Count ?? 0),
                        SchoolCount = levelTeams
                            .Where(t => t.school_code.HasValue)
                            .Select(t => t.school_code!.Value)
                            .Distinct()
                            .Count()
                    };
                })
                .OrderByDescending(x => x.TeamCount)
                .ThenBy(x => x.CompetitionName)
                .ThenBy(x => x.LevelName)
                .ToList();

            var result = new DashboardReportDto
            {
                TotalSchools = teams
                    .Where(t => t.school_code.HasValue)
                    .Select(t => t.school_code!.Value)
                    .Distinct()
                    .Count(),
                TotalTeams = teams.Count,
                TotalStudents = teams.Sum(t => t.Students?.Count ?? 0),
                TotalCompetitions = compYears.Count,
                RegisteredTeams = teams.Count(t => t.Status == SD.TeamStatus.Registered),
                VerifiedTeams = teams.Count(t => t.Status == SD.TeamStatus.Verified),
                ProjectRevisionTeams = teams.Count(t => t.Status == SD.TeamStatus.ProjectRevision),
                CheckedInTeams = teams.Count(t => t.Status == SD.TeamStatus.CheckedIn),
                DisqualifiedTeams = teams.Count(t => t.Status == SD.TeamStatus.Disqualified),
                TotalProjects = teams.Count(t =>
                    !string.IsNullOrWhiteSpace(t.ProjectName) ||
                    !string.IsNullOrWhiteSpace(t.ProjectPDFLink) ||
                    t.TeamProjects.Any()),
                TotalCertificates = teamIds.Count == 0
                    ? 0
                    : await _context.TeamCertificates.CountAsync(c => teamIds.Contains(c.TeamID)),
                TeamStatusSummary = teamStatusSummary,
                CompetitionSummary = competitionSummary,
                SchoolSummary = teams
                    .Where(t => t.schools != null)
                    .GroupBy(t => new
                    {
                        t.schools!.school_name,
                        t.schools.province
                    })
                    .Select(g => new SchoolSummaryDto
                    {
                        SchoolName = g.Key.school_name,
                        Province = g.Key.province,
                        TeamCount = g.Count(),
                        StudentCount = g.Sum(t => t.Students?.Count ?? 0)
                    })
                    .OrderByDescending(x => x.TeamCount)
                    .ThenByDescending(x => x.StudentCount)
                    .ThenBy(x => x.SchoolName)
                    .Take(10)
                    .ToList(),
                RewardSummary = teams
                    .Where(t => t.RewardType != null)
                    .GroupBy(t => t.RewardType!.RewardTypeName)
                    .Select(g => new RewardSummaryDto
                    {
                        RewardName = g.Key,
                        TeamCount = g.Count()
                    })
                    .OrderByDescending(x => x.TeamCount)
                    .ThenBy(x => x.RewardName)
                    .ToList()
            };

            return Ok(result);
        }

        private static string GetTeamStatusName(SD.TeamStatus status)
        {
            return status switch
            {
                SD.TeamStatus.Registered => "ทีมที่ลงทะเบียนแล้ว",
                SD.TeamStatus.Verified => "การลงทะเบียนสำเร็จ",
                SD.TeamStatus.ProjectRevision => "รอแก้ไขโครงงาน",
                SD.TeamStatus.CheckedIn => "ทีมที่เช็คอินแล้ว",
                SD.TeamStatus.Disqualified => "ทีมที่ถูกตัดสิทธิ์",
                _ => "ไม่ทราบสถานะ"
            };
        }
    }
}
