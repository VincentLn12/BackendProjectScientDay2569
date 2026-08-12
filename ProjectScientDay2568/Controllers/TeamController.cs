using System.Security.Claims;
using ProjectScientDay2568.Models.Dtos.Team;

namespace ProjectScientDay2568.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TeamController : ControllerBase
    {
        private readonly TeamService _team;
        private readonly UserContextService _userContext;

        public TeamController(TeamService team, UserContextService userContext)
        {
            _team = team;
            _userContext = userContext;
       
        }
        private IActionResult HandleResponse(ApiResponse response)
        {
            return StatusCode((int)response.StatusCode, response);
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var response = new ApiResponse();
            try
            {
                var years = await _team.GetAllTeamAsync();
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

        [HttpPost("copy")]
        public async Task<ActionResult<TeamCopyResultDto>> CopyTeam([FromBody] TeamCopyRequest req)
        {
            if (req == null || req.SourceTeamId <= 0) return BadRequest("ข้อมูลไม่ถูกต้อง");

            // ✅ ดึง Id จาก Claims (รองรับทั้งมาตรฐานและเคลม "id" ที่อยู่ใน JWT ของคุณ)
            var userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier) // มาตรฐานของ ASP.NET Identity
                ?? User.FindFirst("sub")?.Value                 // บาง JWT ใช้ sub
                ?? User.FindFirst("id")?.Value                  // จาก payload ตัวอย่างของคุณ
                ?? null;

            var result = await _team.CopyTeamAsync(req, userId);
            return Ok(result);
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetStatus(int competitionLevelID)
        {
            var response = new ApiResponse();
            try
            {
                var years = await _team.GetAllTeamStatusAsync(competitionLevelID);
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

        [HttpPost]
        public async Task<IActionResult> Post([FromForm] TeamCreateDto form)
        {
            var response = new ApiResponse();

            try
            {
                if (!ModelState.IsValid)
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.IsSuccess = false;
                    response.ErrorMessages.AddRange(ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));
                    return BadRequest(response);
                }

                // กันเคสยังไม่ได้เลือกโรงเรียน
                if (form.school_code is null)
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.IsSuccess = false;
                    response.ErrorMessages.Add("กรุณาเลือกสถานศึกษา (school_code)");
                    return BadRequest(response);
                }

                var userId = _userContext.UserId;

                var createdTeam = await _team.CreateTeamAsync(
                  form,
                  userId,
                  form.File,
                  form.ProjectName
              );

                response.StatusCode = HttpStatusCode.Created;
                response.IsSuccess = true;
                response.Result = createdTeam;
                return StatusCode((int)response.StatusCode, response);
            }
            catch (InvalidOperationException ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.Forbidden;
                response.ErrorMessages.Add(ex.Message);
                return StatusCode((int)response.StatusCode, response);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
                return StatusCode((int)response.StatusCode, response);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] TeamCreateDto teamDto)
        {
            var response = new ApiResponse();
            try
            {
                if (!ModelState.IsValid)
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.IsSuccess = false;
                    response.ErrorMessages.AddRange(ModelState.Values
                                        .SelectMany(v => v.Errors)
                                        .Select(e => e.ErrorMessage));
                    return BadRequest(response);
                }

                var userId = _userContext.UserId;
                var updatedYear = await _team.UpdateTeamAsync(id, teamDto , userId);
                response.StatusCode = HttpStatusCode.OK;
                response.Result = updatedYear;
            }
            catch (InvalidOperationException ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.Forbidden;
                response.ErrorMessages.Add(ex.Message);
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

                if (ex.InnerException != null)
                {
                    response.ErrorMessages.Add("Inner Exception: " + ex.InnerException.Message);
                }

                if (ex.InnerException?.InnerException != null)
                {
                    response.ErrorMessages.Add("Deep Inner Exception: " + ex.InnerException.InnerException.Message);
                }
            }

            return HandleResponse(response);
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var response = new ApiResponse();
            try
            {
                await _team.DeleteTeamAsync(id);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.Forbidden;
                response.ErrorMessages.Add(ex.Message);
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

       

    }
}
