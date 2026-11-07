using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace ProjectScientDay2568.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExportController : ControllerBase
    {
        private readonly ExportService _export;
        private readonly CompetitionlevelService _levels;

        public ExportController(ExportService export, CompetitionlevelService levels)
        {
            _export = export;
            _levels = levels;
        }

        [HttpGet("certificateTeam/{teamId}")]
        public async Task<IActionResult> DownloadCertificate(int teamId)
        {
            try
            {
                await _levels.EnsureCertificateNumberForDownloadAsync(teamId);
                var pdfBytes = await _export.GenerateCertificateTeamAsync(teamId);
                var fileName = $"Certificate_Team_{teamId}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (CertificateNotReadyException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "เกิดข้อผิดพลาดในการสร้างใบเกียรติบัตร", detail = ex.Message });
            }
        }

        [HttpGet("certificateStudent/{teamId}/{studentId}")]
        public async Task<IActionResult> DownloadCertificateStudent(int teamId, int studentId)
        {
            try
            {
                await _levels.EnsureCertificateNumberForDownloadAsync(teamId);
                var pdfBytes = await _export.GenerateCertificateStudentAsync(teamId, studentId);
                var fileName = $"Certificate_Student_{teamId}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (CertificateNotReadyException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "เกิดข้อผิดพลาดในการสร้างใบเกียรติบัตร", detail = ex.Message });
            }
        }
        [HttpGet("trainingSignInSheet/{trainingtopicId}")]
        public async Task<IActionResult> DownloadTrainingSignInSheet(int trainingtopicId)
        {
            try
            {
                var pdfBytes = await _export.GenerateTrainingSignInSheetAsync(trainingtopicId);
                return File(pdfBytes, "application/pdf", $"Training_SignIn_{trainingtopicId}.pdf");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "เกิดข้อผิดพลาดในการสร้างใบลงชื่อผู้เข้าอบรม", detail = ex.Message });
            }
        }

        // เกียรติบัตรผู้เข้าอบรม 1 ใบ: เจ้าของการสมัคร หรือ แอดมิน/เจ้าหน้าที่
        [Authorize]
        [HttpGet("trainingCertificate/{registrationId}")]
        public async Task<IActionResult> DownloadTrainingCertificate(int registrationId)
        {
            var isStaff = User.IsInRole("Admin") || User.IsInRole("admin") || User.IsInRole("Staff") || User.IsInRole("staff") || User.IsInRole("เจ้าหน้าที่");
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? User.FindFirstValue("id");
            return await TrainingCertificateResult(
                () => _export.GenerateTrainingCertificateAsync(registrationId, isStaff ? null : userId ?? ""),
                $"Training_Certificate_{registrationId}.pdf");
        }

        // เกียรติบัตรทุกคนในหลักสูตร (คนละหน้า)
        [Authorize(Roles = "Admin,admin,Staff,staff,เจ้าหน้าที่")]
        [HttpGet("trainingCertificates/{trainingtopicId}")]
        public Task<IActionResult> DownloadTrainingCertificatesForTopic(int trainingtopicId) =>
            TrainingCertificateResult(
                () => _export.GenerateTrainingCertificatesForTopicAsync(trainingtopicId),
                $"Training_Certificates_{trainingtopicId}.pdf");

        private async Task<IActionResult> TrainingCertificateResult(Func<Task<byte[]>> generate, string fileName)
        {
            try
            {
                return File(await generate(), "application/pdf", fileName);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "เกิดข้อผิดพลาดในการสร้างเกียรติบัตร", detail = ex.Message });
            }
        }

        [HttpGet("trainingSignInSheet/year/{yearId}")]
        public async Task<IActionResult> DownloadTrainingSignInSheetsByYear(int yearId)
        {
            try
            {
                var pdfBytes = await _export.GenerateTrainingSignInSheetsByYearAsync(yearId);
                return File(pdfBytes, "application/pdf", $"Training_SignIn_Year_{yearId}.pdf");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "เกิดข้อผิดพลาดในการสร้างใบลงชื่อผู้เข้าอบรม", detail = ex.Message });
            }
        }

        [HttpGet("reportpdfTeam/{competitionLevelId}")]
        public async Task<IActionResult> DownloadTeamReport(int competitionLevelId)
        {
            try
            {
                var pdfBytes = await _export.GenerateTeamReportTableAsync(competitionLevelId); // ✅ เรียกเมธอดที่สร้าง PDF ตาราง
                var fileName = $"Team_Report_Level_{competitionLevelId}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "เกิดข้อผิดพลาดในการสร้างรายงานทีมแข่งขัน", detail = ex.Message });
            }
        }
        [HttpGet("reportwordTeam/{competitionLevelId}")]
        public async Task<IActionResult> DownloadTeamReportWord_OpenXml(int competitionLevelId)
        {
            try
            {
                var wordBytes = await _export.GenerateTeamReportWordTeamAsync(competitionLevelId);
                var fileName = $"Team_Report_Level_{competitionLevelId}.docx";
                return File(wordBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "เกิดข้อผิดพลาดในการสร้างรายงาน Word", detail = ex.Message });
            }
        }

        [HttpGet("reportwordCompetitionYear/{competitionYearId}")]
        public async Task<IActionResult> DownloadTeamReportWordByCompetitionYear(int competitionYearId)
        {
            try
            {
                var wordBytes = await _export.GenerateTeamReportWordByCompetitionYearAsync(competitionYearId);

                var fileName = $"Team_Report_CompetitionYear_{competitionYearId}.docx";

                return File(
                    wordBytes,
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    fileName
                );
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "เกิดข้อผิดพลาดในการสร้างรายงาน Word รวมทุกระดับชั้น",
                    detail = ex.Message
                });
            }
        }

        [HttpPost("reportwordCompetitionLevels")]
        public async Task<IActionResult> DownloadTeamReportWordByCompetitionLevels(
            [FromBody] ProjectScientDay2568.Models.Dtos.Report.CompetitionLevelExportRequestDto request)
        {
            try
            {
                if (request?.CompetitionLevelIds == null || request.CompetitionLevelIds.Count == 0)
                {
                    return BadRequest(new { message = "กรุณาเลือกระดับชั้นอย่างน้อย 1 รายการ" });
                }

                var wordBytes = await _export.GenerateTeamReportWordByCompetitionLevelsAsync(request.CompetitionLevelIds);
                var fileName = $"Team_Report_Selected_Levels_{DateTime.Now:yyyyMMdd_HHmmss}.docx";

                return File(
                    wordBytes,
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    fileName
                );
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "เกิดข้อผิดพลาดในการสร้างรายงาน Word ตามระดับชั้นที่เลือก",
                    detail = ex.Message
                });
            }
        }

        [HttpGet("reportwordCompetitionYearsSummary/{yearId}")]
        public async Task<IActionResult> DownloadCompetitionYearSummaryWord(int yearId)
        {
            try
            {
                var wordBytes = await _export.GenerateCompetitionYearSummaryWordAsync(yearId);
                var fileName = $"Competition_Summary_{yearId}_{DateTime.Now:yyyyMMdd_HHmmss}.docx";

                return File(
                    wordBytes,
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    fileName
                );
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "เกิดข้อผิดพลาดในการสร้างรายงานสรุปกิจกรรม",
                    detail = ex.Message
                });
            }
        }

        [HttpGet("reportpdfCompetitionYearsSummary/{yearId}")]
        public async Task<IActionResult> DownloadCompetitionYearSummaryPdf(int yearId)
        {
            try
            {
                var pdfBytes = await _export.GenerateCompetitionYearSummaryPdfAsync(yearId);
                var fileName = $"Competition_Summary_{yearId}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

                return File(
                    pdfBytes,
                    "application/pdf",
                    fileName
                );
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "เกิดข้อผิดพลาดในการสร้างรายงานสรุปกิจกรรม PDF",
                    detail = ex.Message
                });
            }
        }

        [HttpGet("reportpdfCompetitionAttendance/{yearId}")]
        public async Task<IActionResult> DownloadCompetitionAttendanceReportPdf(int yearId)
        {
            try
            {
                var pdfBytes = await _export.GenerateCompetitionAttendanceReportPdfAsync(yearId);
                var fileName = $"Competition_Attendance_Report_{yearId}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "เกิดข้อผิดพลาดในการสร้างรายงานผู้เข้างาน PDF", detail = ex.Message });
            }
        }

        [HttpGet("winners-form-word/{competitionLevelId}")]
        public async Task<IActionResult> WinnersFormWord(int competitionLevelId)
        {
            try
            {
                var word = await _export.GenerateWinnersFormWordAsync(competitionLevelId);
                return File(word,
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    $"Winners_Level_{competitionLevelId}.docx");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "เกิดข้อผิดพลาดในการสร้างรายงานผู้ชนะ Word", detail = ex.Message });
            }
        }

        [HttpGet("winners-form-word/year/{yearId}")]
        public async Task<IActionResult> WinnersFormByYearWord(int yearId)
        {
            try
            {
                var word = await _export.GenerateWinnersFormByYearWordAsync(yearId);
                return File(word,
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    $"Winners_Year_{yearId}.docx");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "เกิดข้อผิดพลาดในการสร้างรายงานผู้ชนะประจำปี Word", detail = ex.Message });
            }
        }

        [HttpGet("winners-form/{competitionLevelId}")]
        public async Task<IActionResult> WinnersForm(int competitionLevelId)
        {
            var pdf = await _export.GenerateWinnersFormAsync( competitionLevelId );
            return File(pdf, "application/pdf", "winners.pdf");
        }

        [HttpGet("winners-form/year/{yearId}")]
        public async Task<IActionResult> WinnersFormByYear(int yearId)
        {
            try
            {
                var pdf = await _export.GenerateWinnersFormByYearAsync(yearId);
                return File(pdf, "application/pdf", $"Winners_{yearId}.pdf");
            }
            catch (KeyNotFoundException ex) 
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "เกิดข้อผิดพลาดในการสร้างรายงานผู้ชนะประจำปี",
                    detail = ex.Message
                });
            }
        }
    }
}
