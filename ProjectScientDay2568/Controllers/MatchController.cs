 using ProjectScientDay2568.Models.Dtos.Match;

namespace ProjectScientDay2568.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MatchController : ControllerBase
    {
        private readonly MatchService _matchService;
        public MatchController(MatchService matchService)
        {
            _matchService = matchService;
        }

        [HttpGet("competition-level/{competitionLevelID}/matches")]
        public async Task<IActionResult> GetMatches(int competitionLevelID )
        {
            var response = new ApiResponse();
            try
            {
                var matches = await _matchService.GetMatchesByCompetitionLevelIdAsync(competitionLevelID);
                response.StatusCode = HttpStatusCode.OK;
                response.Result = matches;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add(ex.Message);
            }
            return StatusCode((int)response.StatusCode, response);
        }
 
        [HttpPost("competition-level/{competitionLevelID}/generate")]
        public async Task<IActionResult> GenerateBracket(int competitionLevelID ,[FromQuery] int? sampleSize = null)
        {
            var response = new ApiResponse();
            try
            {
                var matches = await _matchService.GenerateMatchBracketAsync(competitionLevelID ,sampleSize);
                response.StatusCode = HttpStatusCode.Created;
                response.Result = matches;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }
            return StatusCode((int)response.StatusCode, response);
        }

        [HttpPost("competition-level/{competitionLevelID}/start-next-round")]
        public async Task<IActionResult> GenerateNextRound(int competitionLevelID)
        {
            var response = new ApiResponse();
            try
            {
                var message = await _matchService.GenerateNextRoundAutoAsync(competitionLevelID);
                response.StatusCode = HttpStatusCode.Created;
                response.Result = message;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }

            return StatusCode((int)response.StatusCode, response);
        }

        //[HttpGet("competition-level/{competitionLevelID}/summary")]
        //public async Task<IActionResult> SummaryTournament(int competitionLevelID)
        //{
        //    var response = new ApiResponse();
        //    try
        //    {
        //        var resultMessage = await _matchService.GetTournamentSummaryAsync(competitionLevelID);
        //        response.StatusCode = HttpStatusCode.OK;
        //        response.Result = resultMessage;
        //    }
        //    catch (Exception ex)
        //    {
        //        response.IsSuccess = false;
        //        response.StatusCode = HttpStatusCode.InternalServerError;
        //        response.ErrorMessages.Add(ex.Message);
        //    }

        //    return StatusCode((int)response.StatusCode, response);
        //}

        //[HttpGet("competition-level/{competitionLevelID}/finalize")]
        //public async Task<IActionResult> FinalizeTournament(int competitionLevelID)
        //{
        //    var response = new ApiResponse();
        //    try
        //    {
        //        var resultMessage = await _matchService.FinalizeTournamentAsync(competitionLevelID);
        //        response.StatusCode = HttpStatusCode.OK;
        //        response.Result = resultMessage;
        //    }
        //    catch (Exception ex)
        //    {
        //        response.IsSuccess = false;
        //        response.StatusCode = HttpStatusCode.InternalServerError;
        //        response.ErrorMessages.Add(ex.Message);
        //    }

        //    return StatusCode((int)response.StatusCode, response);
        //}

        [HttpDelete("competition-level/{competitionLevelID}/cancel-round/{roundNumber}")]
        public async Task<IActionResult> CancelRoundMatch(int competitionLevelID, int roundNumber)
        {
            var response = new ApiResponse();
            try
            {
                var updatedMatch = await _matchService.CancelRoundAsync(competitionLevelID, roundNumber);
                response.IsSuccess = true;
                response.StatusCode = HttpStatusCode.OK;
                response.Result = updatedMatch;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }

            return StatusCode((int)response.StatusCode, response);
        }

        [HttpPut("match/{matchId}/submit")]
        public async Task<IActionResult> SubmitResult(int matchId, SubmitMatchResultDto dto)
        {
            var response = new ApiResponse();
            try
            {
                var updatedMatch = await _matchService.SubmitResultAsync(matchId, dto.WinnerId, dto.IsEdit);
                response.StatusCode = HttpStatusCode.OK;
                response.Result = updatedMatch;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }

            return StatusCode((int)response.StatusCode, response);
        }
    }
}
