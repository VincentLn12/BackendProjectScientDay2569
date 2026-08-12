using ProjectScientDay2568.Models.Dtos.Report;

namespace ProjectScientDay2568.Controllers
{
    [Route("api/public/report")]
    [ApiController]
    public class PublicReportController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PublicReportController(ApplicationDbContext context)
        {
            _context = context;
        }

        private IActionResult HandleResponse(ApiResponse response)
        {
            return StatusCode((int)response.StatusCode, response);
        }

        [HttpGet("years")]
        public async Task<IActionResult> GetYears()
        {
            var response = new ApiResponse();
            try
            {
                var result = await _context.Years
                    .AsNoTracking()
                    .Where(y => y.isActive)
                    .OrderByDescending(y => y.YearNumber)
                    .ThenByDescending(y => y.YearID)
                    .Select(y => new PublicReportYearDto
                    {
                        YearID = y.YearID,
                        YearNumber = y.YearNumber,
                        YearName = y.YearName,
                        IsActive = y.isActive
                    })
                    .ToListAsync();

                response.StatusCode = HttpStatusCode.OK;
                response.Result = result;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }

            return HandleResponse(response);
        }

        [HttpGet("competition-years")]
        public async Task<IActionResult> GetCompetitionYears([FromQuery] int yearId)
        {
            var response = new ApiResponse();
            try
            {
                var result = await _context.CompetitionYears
                    .AsNoTracking()
                    .Include(cy => cy.Competition)
                    .Where(cy => cy.YearID == yearId)
                    .OrderBy(cy => cy.Competition!.CompetitionName)
                    .Select(cy => new PublicReportCompetitionYearDto
                    {
                        CompetitionYearID = cy.CompetitionYearID,
                        YearID = cy.YearID,
                        CompetitionID = cy.CompetitionID,
                        CompetitionName = cy.Competition!.CompetitionName,
                        RegistrationLocation = cy.RegistrationLocation,
                        RequiresProject = cy.Competition!.RequiresProject,
                        SchoolCount = _context.Teams
                            .Where(t => t.CompetitionLevel != null &&
                                        t.CompetitionLevel.CompetitionYearID == cy.CompetitionYearID &&
                                        t.CompetitionLevelID.HasValue &&
                                        t.school_code.HasValue)
                            .Select(t => new
                            {
                                CompetitionLevelID = t.CompetitionLevelID!.Value,
                                SchoolCode = t.school_code!.Value
                            })
                            .Distinct()
                            .Count(),
                        StudentCount = _context.Teams
                            .Where(t => t.CompetitionLevel!.CompetitionYearID == cy.CompetitionYearID)
                            .SelectMany(t => t.Students)
                            .Count()
                    })
                    .ToListAsync();

                response.StatusCode = HttpStatusCode.OK;
                response.Result = result;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }

            return HandleResponse(response);
        }

        [HttpGet("competition-levels")]
        public async Task<IActionResult> GetCompetitionLevels([FromQuery] int competitionYearID)
        {
            var response = new ApiResponse();
            try
            {
                var result = await _context.CompetitionLevels
                    .AsNoTracking()
                    .Include(cl => cl.EducationLevel)
                    .Where(cl => cl.CompetitionYearID == competitionYearID)
                    .OrderBy(cl => cl.EducationLevel!.LevelName)
                    .Select(cl => new PublicReportCompetitionLevelDto
                    {
                        CompetitionLevelID = cl.CompetitionLevelID,
                        CompetitionYearID = cl.CompetitionYearID,
                        LevelID = cl.LevelID,
                        LevelName = cl.EducationLevel!.LevelName,
                        SchoolCount = _context.Teams
                            .Where(t => t.CompetitionLevelID == cl.CompetitionLevelID && t.school_code.HasValue)
                            .Select(t => t.school_code!.Value)
                            .Distinct()
                            .Count(),
                        StudentCount = _context.Teams
                            .Where(t => t.CompetitionLevelID == cl.CompetitionLevelID)
                            .SelectMany(t => t.Students)
                            .Count(),
                        CompetitionDateTime = cl.CompetitionDateTime
                    })
                    .ToListAsync();

                response.StatusCode = HttpStatusCode.OK;
                response.Result = result;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }

            return HandleResponse(response);
        }

        [HttpGet("competition-level-summary")]
        public async Task<IActionResult> GetCompetitionLevelSummary([FromQuery] int competitionLevelID)
        {
            var response = new ApiResponse();
            try
            {
                var level = await _context.CompetitionLevels
                    .AsNoTracking()
                    .Include(cl => cl.EducationLevel)
                    .Include(cl => cl.CompetitionYear)
                        .ThenInclude(cy => cy!.Year)
                    .Include(cl => cl.CompetitionYear)
                        .ThenInclude(cy => cy!.Competition)
                    .FirstOrDefaultAsync(cl => cl.CompetitionLevelID == competitionLevelID);

                if (level == null)
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.ErrorMessages.Add("Competition level not found.");
                    return HandleResponse(response);
                }

                var teams = await _context.Teams
                    .AsNoTracking()
                    .Include(t => t.Students)
                    .Where(t => t.CompetitionLevelID == competitionLevelID)
                    .ToListAsync();

                var result = new PublicReportCompetitionLevelSummaryDto
                {
                    CompetitionLevelID = level.CompetitionLevelID,
                    CompetitionYearID = level.CompetitionYearID,
                    YearID = level.CompetitionYear!.YearID,
                    YearName = level.CompetitionYear.Year!.YearName,
                    YearNumber = level.CompetitionYear.Year.YearNumber,
                    CompetitionName = level.CompetitionYear.Competition!.CompetitionName,
                    LevelName = level.EducationLevel!.LevelName,
                    RequiresProject = level.CompetitionYear.Competition!.RequiresProject,
                    RegistrationLocation = level.CompetitionYear.RegistrationLocation,
                    CompetitionDateTime = level.CompetitionDateTime,
                    SchoolCount = teams
                        .Where(t => t.school_code.HasValue)
                        .Select(t => t.school_code!.Value)
                        .Distinct()
                        .Count(),
                    TeamCount = teams.Count,
                    StudentCount = teams.Sum(t => t.Students?.Count ?? 0)
                };

                response.StatusCode = HttpStatusCode.OK;
                response.Result = result;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }

            return HandleResponse(response);
        }

        [HttpGet("teams")]
        public async Task<IActionResult> GetTeams(
            [FromQuery] int competitionLevelID,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null)
        {
            var response = new ApiResponse();
            try
            {
                if (page <= 0) page = 1;
                if (pageSize <= 0) pageSize = 20;

                var query = _context.Teams
                    .AsNoTracking()
                    .Include(t => t.schools)
                    .Include(t => t.Students)
                    .Include(t => t.TeamProjects)
                    .Where(t => t.CompetitionLevelID == competitionLevelID);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.Trim();
                    query = query.Where(t =>
                        (t.schools != null && t.schools.school_name.Contains(search)) ||
                        (t.ProjectName != null && t.ProjectName.Contains(search)) ||
                        t.TeamProjects.Any(tp => tp.ProjectName.Contains(search)) ||
                        (t.PrimaryFirstName != null && t.PrimaryFirstName.Contains(search)) ||
                        (t.PrimaryLastName != null && t.PrimaryLastName.Contains(search)));
                }

                var total = await query.CountAsync();

                var items = await query
                    .OrderBy(t => t.schools != null ? t.schools.school_name : string.Empty)
                    .ThenBy(t => t.TeamID)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(t => new PublicReportTeamDto
                    {
                        TeamID = t.TeamID,
                        SchoolName = t.schools != null ? t.schools.school_name : "-",
                        Province = t.schools != null ? t.schools.province : null,
                        District = t.schools != null ? t.schools.district : null,
                        TeamName = t.NameOrdinal.HasValue
                            ? "ทีมลำดับที่ " + t.NameOrdinal.Value
                            : null,
                        ProjectName = t.TeamProjects
                            .OrderByDescending(tp => tp.TeamProjectID)
                            .Select(tp => tp.ProjectName)
                            .FirstOrDefault() ?? t.ProjectName,
                        ProjectPDFLink = t.TeamProjects
                            .OrderByDescending(tp => tp.TeamProjectID)
                            .Select(tp => tp.ProjectPDFLink)
                            .FirstOrDefault() ?? t.ProjectPDFLink,
                        TeacherName =
                            ((t.PrimaryFirstName ?? string.Empty) +
                            (((t.PrimaryFirstName ?? string.Empty) != string.Empty &&
                              (t.PrimaryLastName ?? string.Empty) != string.Empty)
                                ? " "
                                : string.Empty) +
                            (t.PrimaryLastName ?? string.Empty)),
                        StudentCount = t.Students.Count,
                        MainStudentCount = t.Students.Count(s => s.IsMainMember),
                        ReserveStudentCount = t.Students.Count(s => !s.IsMainMember)
                    })
                    .ToListAsync();

                response.StatusCode = HttpStatusCode.OK;
                response.Result = new PublicReportPagedTeamsDto
                {
                    Items = items,
                    Total = total,
                    Page = page,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }

            return HandleResponse(response);
        }

        [HttpGet("teams/{teamId}/students")]
        public async Task<IActionResult> GetStudentsByTeam(int teamId)
        {
            var response = new ApiResponse();
            try
            {
                var exists = await _context.Teams
                    .AsNoTracking()
                    .AnyAsync(t => t.TeamID == teamId);

                if (!exists)
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.ErrorMessages.Add("Team not found.");
                    return HandleResponse(response);
                }

                var result = await _context.Students
                    .AsNoTracking()
                    .Where(s => s.TeamID == teamId)
                    .OrderByDescending(s => s.IsMainMember)
                    .ThenBy(s => s.FirstName)
                    .ThenBy(s => s.LastName)
                    .Select(s => new PublicReportStudentDto
                    {
                        StudentID = s.StudentID,
                        FirstName = s.FirstName,
                        LastName = s.LastName,
                        TitlePrefixLabel = s.TitlePrefix.ToString(),
                        IsMainMember = s.IsMainMember
                    })
                    .ToListAsync();

                response.StatusCode = HttpStatusCode.OK;
                response.Result = result;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }

            return HandleResponse(response);
        }
    }
}
