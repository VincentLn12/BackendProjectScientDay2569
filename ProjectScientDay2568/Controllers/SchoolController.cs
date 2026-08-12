using ProjectScientDay2568.Models.Dtos.Team;

namespace ProjectScientDay2568.Controllers
{
    [Route("api/schools")] 
    [ApiController]
    public class SchoolController : ControllerBase
    {
        private readonly SchoolService _school;

        public SchoolController(SchoolService school)
        {
           _school = school;
        }

        private IActionResult HandleResponse(ApiResponse response)
        {
            return StatusCode((int)response.StatusCode, response);
        }
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string? query, [FromQuery] int? limit)
        {
            var response = new ApiResponse { IsSuccess = true, ErrorMessages = new List<string>() };
            try
            {
                if (!string.IsNullOrWhiteSpace(query))
                {
                    var list = await _school.SearchSchoolsAsync(query, limit ?? 50);
                    response.StatusCode = HttpStatusCode.OK;
                    response.Result = list;
                }
                else
                {
                    var list = await _school.GetSchool();
                    response.StatusCode = HttpStatusCode.OK;
                    response.Result = list;
                }
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }
            return HandleResponse(response);
        }

        // GET: api/schools/{code}
        [HttpGet("{code:int}")]
        public async Task<IActionResult> GetByCode(int code)
        {
            var response = new ApiResponse { IsSuccess = true, ErrorMessages = new List<string>() };
            try
            {
                var school = await _school.GetByCodeAsync(code);
                if (school == null)
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.ErrorMessages.Add("ไม่พบโรงเรียน");
                }
                else
                {
                    response.StatusCode = HttpStatusCode.OK;
                    response.Result = school;
                }
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
        public async Task<IActionResult> Post([FromBody] SchoolDto schoolDto)
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

                var createdYear = await _school.CreateSchoolAsync(schoolDto);
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

        [HttpPut("{schoolCode}")]
        public async Task<IActionResult> Put(int schoolCode, [FromBody] SchoolDto schoolDto)
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
                var updatedYear = await _school.UpdateSchooAsync(schoolCode, schoolDto);
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
                await _school.DeleteSchooAsync(id);
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

        [HttpGet("provinces")]
        public async Task<IActionResult> GetProvinces()
        {
            var response = new ApiResponse { IsSuccess = true, ErrorMessages = new List<string>() };
            try
            {
                var list = await _school.GetProvincesAsync();
                response.StatusCode = HttpStatusCode.OK;
                response.Result = list;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }
            return HandleResponse(response);
        }

        [HttpGet("districts")]
        public async Task<IActionResult> GetDistricts([FromQuery] string province)
        {
            var response = new ApiResponse { IsSuccess = true, ErrorMessages = new List<string>() };
            try
            {
                var list = await _school.GetDistrictsByProvinceAsync(province);
                response.StatusCode = HttpStatusCode.OK;
                response.Result = list;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }
            return HandleResponse(response);
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchByProvinceDistrict([FromQuery] string province, [FromQuery] string district, [FromQuery] string? q, [FromQuery] int? limit)
        {
            var response = new ApiResponse { IsSuccess = true, ErrorMessages = new List<string>() };
            try
            {
                var list = await _school.SearchByProvinceDistrictAsync(province, district, q, limit ?? 100);
                response.StatusCode = HttpStatusCode.OK;
                response.Result = list;
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
