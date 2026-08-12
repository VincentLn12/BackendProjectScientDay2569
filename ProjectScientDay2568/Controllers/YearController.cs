using ProjectScientDay2568.Models.Dtos;
using ProjectScientDay2568.Models.Dtos.Team;

namespace ProjectScientDay2568.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class YearController : ControllerBase
    {
        private readonly YearService _yearService;

        public YearController(YearService yearService)
        {
            _yearService = yearService;
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
                var years = await _yearService.GetAllYearsAsync();
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

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var response = new ApiResponse();
            try
            {
                var year = await _yearService.GetYearByIdAsync(id);
                response.StatusCode = HttpStatusCode.OK;
                response.Result = year;
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

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] YearDto yearDto)
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

                var createdYear = await _yearService.CreateYearAsync(yearDto);
                response.StatusCode = HttpStatusCode.Created;
                response.Result = createdYear;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }
            return HandleResponse(response);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] YearDto yearDto)
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
                var updatedYear = await _yearService.UpdateYearAsync(id, yearDto);
                response.StatusCode = HttpStatusCode.OK;
                response.Result = updatedYear;
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
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var response = new ApiResponse();
            try
            {
                await _yearService.DeleteYearAsync(id);
                return NoContent();
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
       
         [HttpPost("teamcertificateNumber/{yearID}")]
        public async Task<IActionResult> PostTable(int yearID)
        {
            var response = new ApiResponse();
            try
            {
                var certificates = await _yearService.GenerateTeamCertificatesNumber(yearID );
                response.StatusCode = HttpStatusCode.OK;
                response.Result = certificates;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }
            return HandleResponse(response);
        }

        [HttpPost("{id:int}/clone")]
        public async Task<ActionResult<YearDto>> CloneYear(int id, [FromBody] CloneYearRequest req, CancellationToken ct)
        {
            if (id != req.SourceYearId)
                return BadRequest("Route id และ SourceYearId ไม่ตรงกัน");

            var result = await _yearService.CloneYearAsync(req, ct);
            return Ok(result);
        }

    }
}