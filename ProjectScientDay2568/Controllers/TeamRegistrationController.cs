
using ProjectScientDay2568.Models.Dtos.Team;
using static ProjectScientDay2568.Utility.SD;

namespace ProjectScientDay2568.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TeamRegistrationController : ControllerBase
    {
        private readonly TeamService _team;
        private readonly UserContextService _userContext;

        public TeamRegistrationController(TeamService team, UserContextService userContext)
        {
            _team = team;
            _userContext = userContext;
        }

        private IActionResult HandleResponse(ApiResponse response)
        {
            return StatusCode((int)response.StatusCode, response);
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMyRegistrations()
        {
            var response = new ApiResponse();
            try
            {
                // UserContextService.UserId โยน exception ถ้ายังไม่ได้ล็อกอิน
                var userId = _userContext.UserId;
                response.StatusCode = HttpStatusCode.OK;
                response.Result = await _team.GetMyRegistrationsAsync(userId);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }
            return HandleResponse(response);
        }

        [HttpGet("teamcompetitionLevel")]
        public async Task<IActionResult> GetTeamcompetitionLevel(int competitionLevelID)
        {
            var response = new ApiResponse();
            try
            {
                var userId = _userContext.UserId;
                var years = await _team.GetAllTeamcompetitionLevelAsync(competitionLevelID, userId);
                response.StatusCode = HttpStatusCode.OK;
                response.Result = years;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }
            return HandleResponse(response);
        }


        [HttpPut("{teamId}/changStatusTeam")]
        public async Task<IActionResult> CheckIn(int teamId, [FromQuery] TeamStatus status)
        {
            var response = new ApiResponse();
            try
            {
                var result = await _team.ChangStatusteam(teamId, status);
                response.StatusCode = HttpStatusCode.OK;
                response.Result = result;
            }
            catch (KeyNotFoundException knfEx)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.NotFound;
                response.ErrorMessages.Add(knfEx.Message);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }
            return HandleResponse(response);
        }

        [HttpPut("{teamId}/award")]
        public async Task<IActionResult> Award(int teamId, int rewardTypeID)
        {
            var response = new ApiResponse();
            try
            {
                var result = await _team.TeamAward(teamId, rewardTypeID);
                response.StatusCode = HttpStatusCode.OK;
                response.Result = result;
            }
            catch (KeyNotFoundException knfEx)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.NotFound;
                response.ErrorMessages.Add(knfEx.Message);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }
            return HandleResponse(response);
        }

        [HttpPut("register")]
        public async Task<IActionResult> PutRegister(int teamId, [FromBody] TeamRegisterDto teamDto, [FromQuery] int competitionLevelID)
        {
            var response = new ApiResponse();
            try
            {
                if (teamDto == null || !ModelState.IsValid)
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.IsSuccess = false;
                    response.ErrorMessages.AddRange(ModelState.Values
                                          .SelectMany(v => v.Errors)
                                          .Select(e => e.ErrorMessage));
                    return BadRequest(response);
                }

                var userId = _userContext.UserId;

                var createdTeam = await _team.UpdateTeamRegistrationAsync(teamId, userId, teamDto, competitionLevelID);
                response.StatusCode = HttpStatusCode.Created;
                response.Result = createdTeam;
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
