using static ProjectScientDay2568.Utility.SD;
using ProjectScientDay2568.Models.Dtos.Team;

namespace ProjectScientDay2568.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CertificateConfigController : ControllerBase
    {
        private readonly CertificateConfigService _service;

        public CertificateConfigController(CertificateConfigService service)
        {
            _service = service;
        }

        private IActionResult HandleResponse(ApiResponse response)
        {
            return StatusCode((int)response.StatusCode, response);
        }

        private static string? ToClientPath(HttpRequest req, string? p)
        {
            if (string.IsNullOrWhiteSpace(p)) return null;
            if (p.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return p;

            var rel = p.StartsWith("/") ? p : "/" + p; // ให้เป็น path เริ่มด้วย /
            var basePath = req.PathBase.HasValue ? req.PathBase.Value!.TrimEnd('/') : "";
            return $"{basePath}{rel}";
        }

        private static string? ToAbs(HttpRequest req, string? p)
        {
            var clientPath = ToClientPath(req, p);
            if (clientPath == null) return null;
            if (clientPath.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return clientPath;
            return $"{req.Scheme}://{req.Host}{clientPath}";
        }

        [HttpGet("{yearId}/certificate-config")]
        public async Task<IActionResult> GetConfig(int yearId, CancellationToken ct)
        {
            var dto = await _service.GetCertificateConfigAsync(yearId, ct);
            if (dto == null) return Ok(null);

            var bg = await _service.GetCertificateBackgroundWebPathAsync(yearId, ct);

            dto.UniversityDeanSignature = ToAbs(Request, dto.UniversityDeanSignature);
            dto.FacultyDeanSignature = ToAbs(Request, dto.FacultyDeanSignature);
            dto.UniversityLogoPath = ToAbs(Request, dto.UniversityLogoPath);
            dto.InstituteLogoPath = ToAbs(Request, dto.InstituteLogoPath);

            dto.CertificateBackgroundPath = ToAbs(Request, bg);

            return Ok(dto);
        }

        [HttpPost("{yearId:int}/certificate-config")]
        public async Task<IActionResult> CreateCertificateConfig(int yearId, [FromBody] CertificateConfigDto dto, CancellationToken ct)
        {
            var response = new ApiResponse();
            try
            {
                if (!ModelState.IsValid)
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.IsSuccess = false;
                    response.ErrorMessages.AddRange(
                        ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return BadRequest(response);
                }

                dto.YearID = yearId; // บังคับใช้ yearId จาก route
                var created = await _service.CreateCertificateConfigAsync(dto, ct);
                response.StatusCode = HttpStatusCode.Created;
                response.Result = created;
            }
            catch (KeyNotFoundException knfEx) // ปีไม่มี
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.NotFound;
                response.ErrorMessages.Add(knfEx.Message);
            }
            catch (InvalidOperationException invEx) // ซ้ำปี
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.Conflict;
                response.ErrorMessages.Add(invEx.Message);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }
            return HandleResponse(response);
        }
        [HttpPut("{yearId:int}/certificate-config")]
        public async Task<IActionResult> UpdateCertificateConfig(int yearId, [FromBody] UpdateCertificateConfigRequest dto, CancellationToken ct)
        {
            var response = new ApiResponse();
            try
            {
                if (!ModelState.IsValid)
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.IsSuccess = false;
                    response.ErrorMessages.AddRange(
                        ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return BadRequest(response);
                }

                var updated = await _service.UpdateCertificateConfigAsync(yearId, dto, ct);
                response.StatusCode = HttpStatusCode.OK;
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
        [HttpPost("background")]
        public async Task<IActionResult> UploadBackground(int yearId, IFormFile file, CancellationToken ct)
        {
            var path = await _service.UploadCertificateBackgroundAsync(yearId, file, ct);
            return Ok(new { path });
        }
        [HttpPost("signatures/{role}")]
        public async Task<IActionResult> UploadSignature(int yearId, SignatureRole role, IFormFile file, CancellationToken ct)
        {
            var path = await _service.UploadSignatureAsync(yearId, file, role, ct);
            return Ok(new { path });
        }
        [HttpPost("logo/{role}")]
        public async Task<IActionResult> UploadLogo(int yearId, LogoRole role, IFormFile file, CancellationToken ct)
        {
            var path = await _service.UploadLogoAsync(yearId, file, role, ct);
            return Ok(new { path });
        }
    }

}