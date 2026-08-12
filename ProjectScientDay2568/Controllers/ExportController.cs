namespace ProjectScientDay2568.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExportController : ControllerBase
    {
        private readonly ExportService _export;

        public ExportController(ExportService export)
        {
            _export = export;
        }

        [HttpGet("certificateTeam/{teamId}")]
        public async Task<IActionResult> DownloadCertificate(int teamId)
        {
            try
            {
                var pdfBytes = await _export.GenerateCertificateTeamAsync(teamId);
                var fileName = $"Certificate_Team_{teamId}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
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
                var pdfBytes = await _export.GenerateCertificateStudentAsync(teamId, studentId);
                var fileName = $"Certificate_Student_{teamId}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
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

        [HttpGet("winners-form/{competitionLevelId}")]
        public async Task<IActionResult> WinnersForm(int competitionLevelId)
        {
            var pdf = await _export.GenerateWinnersFormAsync( competitionLevelId );
            return File(pdf, "application/pdf", "winners.pdf");
        }
    }
}
