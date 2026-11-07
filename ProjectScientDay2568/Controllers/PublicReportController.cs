using ProjectScientDay2568.Utility;
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
        [HttpGet("level/{levelId}/announcements")]
        public async Task<IActionResult> GetCompetitorAnnouncements(int levelId)
        {
            var response = new ApiResponse();
            try
            {
                var level = await _context.CompetitionLevels
                    .AsNoTracking()
                    .Include(cl => cl.EducationLevel)
                    .Include(cl => cl.CompetitionYear)
                    .ThenInclude(cy => cy!.Competition)
                    .FirstOrDefaultAsync(cl => cl.CompetitionLevelID == levelId);

                if (level == null)
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.ErrorMessages.Add("ไม่พบระดับชั้นการแข่งขันที่ระบุ");
                    return HandleResponse(response);
                }

                var compName = level.CompetitionYear?.Competition?.CompetitionName ?? "";
                var eduName = level.EducationLevel?.LevelName ?? "";
                var levelName = $"{compName} ({eduName})".Trim();

                var teams = await _context.Teams
                    .AsNoTracking()
                    .Include(t => t.schools)
                    .Include(t => t.Students)
                    .Include(t => t.Advisors)
                    .Where(t => t.CompetitionLevelID == levelId && (t.Status == SD.TeamStatus.Verified || t.Status == SD.TeamStatus.CheckedIn || t.Status == SD.TeamStatus.Registered))
                    .OrderBy(t => t.schools != null ? t.schools.school_name : string.Empty)
                    .ThenBy(t => t.TeamID)
                    .ToListAsync();

                var result = new List<CompetitorAnnouncementDto>();

                foreach (var t in teams)
                {
                    var dto = new CompetitorAnnouncementDto
                    {
                        TeamID = t.TeamID,
                        SchoolName = t.schools != null ? t.schools.school_name : "ไม่ระบุสถานศึกษา",
                        CompetitionLevelName = levelName,
                        Remarks = t.Status == SD.TeamStatus.CheckedIn ? "ลงทะเบียนเข้างานแล้ว" : "ผ่านการตรวจสอบ (มีสิทธิ์แข่งขัน)",
                    };

                    if (t.Students != null && t.Students.Any())
                    {
                        dto.CompetitorNames = t.Students
                            .OrderByDescending(s => s.IsMainMember)
                            .ThenBy(s => s.StudentID)
                            .Select(s => $"{s.TitlePrefix} {s.FirstName} {s.LastName}".Trim())
                            .ToList();
                    }

                    var advisors = new List<string>();

                    if (!string.IsNullOrWhiteSpace(t.PrimaryFirstName))
                    {
                        advisors.Add($"{t.PrimaryTitlePrefix} {t.PrimaryFirstName} {t.PrimaryLastName}".Trim());
                    }
                    if (!string.IsNullOrWhiteSpace(t.SecondaryFirstName))
                    {
                        advisors.Add($"{t.SecondaryTitlePrefix} {t.SecondaryFirstName} {t.SecondaryLastName}".Trim());
                    }
                    if (!string.IsNullOrWhiteSpace(t.ThirdFirstName))
                    {
                        advisors.Add($"{t.ThirdTitlePrefix} {t.ThirdFirstName} {t.ThirdLastName}".Trim());
                    }
                    if (!string.IsNullOrWhiteSpace(t.FourthFirstName))
                    {
                        advisors.Add($"{t.FourthTitlePrefix} {t.FourthFirstName} {t.FourthLastName}".Trim());
                    }
                    if (!string.IsNullOrWhiteSpace(t.FifthFirstName))
                    {
                        advisors.Add($"{t.FifthTitlePrefix} {t.FifthFirstName} {t.FifthLastName}".Trim());
                    }

                    if (t.Advisors != null && t.Advisors.Any())
                    {
                        foreach (var adv in t.Advisors)
                        {
                            var advName = $"{adv.TitlePrefix} {adv.FirstName} {adv.LastName}".Trim();
                            if (!string.IsNullOrWhiteSpace(advName) && !advisors.Contains(advName))
                            {
                                advisors.Add(advName);
                            }
                        }
                    }

                    dto.AdvisorNames = advisors;
                    result.Add(dto);
                }

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
