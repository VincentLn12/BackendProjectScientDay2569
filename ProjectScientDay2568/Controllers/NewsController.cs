using Microsoft.AspNetCore.RateLimiting;
using ProjectScientDay2568.Helper;
using ProjectScientDay2568.Models.Dtos;
using ProjectScientDay2568.Models.Dtos.News;

namespace ProjectScientDay2568.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NewsController : ControllerBase
    {
        private readonly NewsService _news;

        public NewsController( NewsService news)
        {
            _news = news;
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
                var rewardType = await _news.GetAllNewsAsync();
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
                var news = await _news.GetNewsByIdAsync(id);
                response.StatusCode = HttpStatusCode.OK;
                response.Result = news;
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
        [RequestSizeLimit(20_000_000)]
        [Consumes("multipart/form-data")]
        [EnableRateLimiting(RateLimitPolicies.Upload)]
        public async Task<IActionResult> Post([FromForm] NewsCreateForm form)
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

                // map form -> DTO (เก็บเฉพาะข้อมูลที่ต้องลง DB)
                var dto = new NewsDto
                {
                    Title = form.Title,
                    Content = form.Content,
                    DatePublished = form.DatePublished
                };

                var created = await _news.CreateNewsAsync(dto, form.CoverImage, form.ContentImages);

                response.StatusCode = HttpStatusCode.Created;
                response.IsSuccess = true;
                response.Result = created;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }
            return HandleResponse(response);
        }

        [HttpPut("{id:int}")]
        [RequestSizeLimit(20_000_000)]
        [Consumes("multipart/form-data")]
        [EnableRateLimiting(RateLimitPolicies.Upload)]
        public async Task<IActionResult> Put(int id, [FromForm] NewsUpdateForm form)
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

                var dto = new NewsDto
                {
                    Title = form.Title,
                    Content = form.Content,
                    DatePublished = form.DatePublished
                };

                var updated = await _news.UpdateNewsAsync(
                    id,
                    dto,
                    form.NewCoverImage,
                    form.NewContentImages,
                    form.RemoveContentImagePaths,
                    form.ReplaceContentImages);

                response.StatusCode = HttpStatusCode.OK;
                response.IsSuccess = true;
                response.Result = updated;
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
                await _news.DeleteNewsAsync(id);
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
