using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectScientDay2568.Data;
using ProjectScientDay2568.Helper;
using ProjectScientDay2568.Models;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace ProjectScientDay2568.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SystemSettingsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public SystemSettingsController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
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
                var setting = await _context.SystemSettings.FirstOrDefaultAsync();
                if (setting == null)
                {
                    // Return a default object if none is set yet
                    setting = new SystemSetting
                    {
                        ProjectName = "คณะวิทยาศาสตร์และเทคโนโลยี",
                        ProjectSubtitle = "มหาวิทยาลัยราชภัฏกาญจนบุรี",
                        ShowNavbar = true,
                        LogoPath = "",
                        Theme = "emerald",
                        ShowTrainingMenu = true,
                        ShowNewsMenu = true,
                        ShowResultsMenu = true,
                        ShowActivityMenu = true,
                        HomepageContent = ""
                    };
                }

                response.StatusCode = HttpStatusCode.OK;
                response.IsSuccess = true;
                response.Result = setting;
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
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Post([FromForm] SystemSettingUpdateDto model)
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

                var setting = await _context.SystemSettings.FirstOrDefaultAsync();
                bool isNew = false;

                if (setting == null)
                {
                    setting = new SystemSetting();
                    isNew = true;
                }

                setting.ProjectName = model.ProjectName ?? "คณะวิทยาศาสตร์และเทคโนโลยี";
                setting.ProjectSubtitle = model.ProjectSubtitle ?? "มหาวิทยาลัยราชภัฏกาญจนบุรี";
                setting.ShowNavbar = model.ShowNavbar;
                setting.Theme = model.Theme ?? "emerald";
                setting.ShowTrainingMenu = model.ShowTrainingMenu;
                setting.ShowNewsMenu = model.ShowNewsMenu;
                setting.ShowResultsMenu = model.ShowResultsMenu;
                setting.ShowActivityMenu = model.ShowActivityMenu;
                setting.HomepageContent = model.HomepageContent;
                setting.HomeHeroTitle = model.HomeHeroTitle;
                setting.HomeHeroHighlight = model.HomeHeroHighlight;
                setting.HomeHeroDescription = model.HomeHeroDescription;
                setting.HomeRegisterButtonLabel = model.HomeRegisterButtonLabel;
                setting.HomeGuideButtonLabel = model.HomeGuideButtonLabel;
                setting.HomeApplicantsButtonLabel = model.HomeApplicantsButtonLabel;
                setting.ShowHomeNews = model.ShowHomeNews;
                setting.HomeNewsTitle = model.HomeNewsTitle;
                setting.HomeNewsDescription = model.HomeNewsDescription;
                setting.HomeNewsButtonLabel = model.HomeNewsButtonLabel;
                setting.HomeDisplayYearId = model.HomeDisplayYearId;

                // Handle Logo upload
                if (model.Logo != null && model.Logo.Length > 0)
                {
                    // Verify extension
                    var ext = Path.GetExtension(model.Logo.FileName).ToLower();
                    var allowedExts = new[] { ".png", ".jpg", ".jpeg", ".webp", ".svg" };
                    if (!allowedExts.Contains(ext))
                    {
                        response.StatusCode = HttpStatusCode.BadRequest;
                        response.IsSuccess = false;
                        response.ErrorMessages.Add("ประเภทไฟล์โลโก้ไม่ถูกต้อง อนุญาตเฉพาะ .png, .jpg, .jpeg, .webp, .svg");
                        return BadRequest(response);
                    }

                    // Delete old logo file if it exists
                    if (!string.IsNullOrEmpty(setting.LogoPath))
                    {
                        FileHelper.DeleteFile(_env.WebRootPath, setting.LogoPath);
                    }

                    // Save new logo file
                    string savedPath = await FileHelper.SaveFileAsync(model.Logo, _env.WebRootPath, new[] { "settings" }, "logo");
                    setting.LogoPath = savedPath;
                }

                if (isNew)
                {
                    _context.SystemSettings.Add(setting);
                }
                else
                {
                    _context.SystemSettings.Update(setting);
                }

                await _context.SaveChangesAsync();

                response.StatusCode = HttpStatusCode.OK;
                response.IsSuccess = true;
                response.Result = setting;
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

    public class SystemSettingUpdateDto
    {
        public string? ProjectName { get; set; }
        public string? ProjectSubtitle { get; set; }
        public bool ShowNavbar { get; set; }
        public IFormFile? Logo { get; set; }
        public string? Theme { get; set; }
        public bool ShowTrainingMenu { get; set; }
        public bool ShowNewsMenu { get; set; }
        public bool ShowResultsMenu { get; set; }
        public bool ShowActivityMenu { get; set; }
        public string? HomepageContent { get; set; }
        public string? HomeHeroTitle { get; set; }
        public string? HomeHeroHighlight { get; set; }
        public string? HomeHeroDescription { get; set; }
        public string? HomeRegisterButtonLabel { get; set; }
        public string? HomeGuideButtonLabel { get; set; }
        public string? HomeApplicantsButtonLabel { get; set; }
        public bool ShowHomeNews { get; set; } = true;
        public string? HomeNewsTitle { get; set; }
        public string? HomeNewsDescription { get; set; }
        public string? HomeNewsButtonLabel { get; set; }
        public int? HomeDisplayYearId { get; set; }
    }
}
