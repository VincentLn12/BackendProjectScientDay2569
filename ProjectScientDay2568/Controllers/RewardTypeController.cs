
namespace ProjectScientDay2568.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RewardTypeController : ControllerBase
    {
        private readonly RewardTypeService _rewardType;

        public RewardTypeController(RewardTypeService rewardType)
        {
            _rewardType = rewardType;
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
                var rewardType = await _rewardType.GetAllRewardTypesAsync();
                response.StatusCode = HttpStatusCode.OK;
                response.Result = rewardType;
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
                var rewardType = await _rewardType.GetRewardTypeByIdAsync(id);
                response.StatusCode = HttpStatusCode.OK;
                response.Result = rewardType;
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
        public async Task<IActionResult> Post([FromBody] RewardTypeDto rewardTypeDto)
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

                var createdrewardType = await _rewardType.CreateRewardTypeAsync(rewardTypeDto);
                response.StatusCode = HttpStatusCode.Created;
                response.Result = createdrewardType;
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
        public async Task<IActionResult> Put(int id, [FromBody] RewardTypeDto rewardTypeDto)
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
                var updatedrewardType = await _rewardType.UpdateRewardTypeAsync(id ,rewardTypeDto);
                response.StatusCode = HttpStatusCode.OK;
                response.Result = updatedrewardType;
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
                await _rewardType.DeleteRewardTypeAsync(id);
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
    }
}
