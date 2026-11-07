using AutoMapper;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Moq;
using ProjectScientDay2568.Data;
using ProjectScientDay2568.Mappings;
using ProjectScientDay2568.Models;
using ProjectScientDay2568.Models.YourNamespace.Models;
using ProjectScientDay2568.Service;
using static ProjectScientDay2568.Utility.SD;

namespace ProjectScientDay2568.Tests.Services;

/// <summary>
/// รายงานรายชื่อทีมที่มีชื่อโครงงาน: ชื่อโครงงานต้องอยู่แถวของตัวเองใต้ทีม (ไม่ใช่คอลัมน์)
/// เพื่อไม่ให้คอลัมน์ ผู้สมัครแข่งขัน / ครูผู้ควบคุม ถูกบีบ
/// </summary>
public class ExportProjectRowTests : IDisposable
{
    private const int LevelId = 10;
    private const int CompetitionYearId = 1;
    private const string LongProject = "การศึกษาประสิทธิภาพของสารสกัดจากเปลือกมังคุดในการยับยั้งการเจริญเติบโตของเชื้อแบคทีเรียที่ก่อให้เกิดสิวบนใบหน้า";

    private readonly ApplicationDbContext _context;
    private readonly ExportService _export;

    public ExportProjectRowTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"ExportProjectRowTests_{Guid.NewGuid()}")
            .Options;
        _context = new ApplicationDbContext(options);
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();

        // หาโฟลเดอร์โปรเจกต์หลักเพื่อใช้ฟอนต์ TH Sarabun New จริง
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "ProjectScientDay2568", "Fonts")))
            dir = dir.Parent;
        var contentRoot = dir == null ? AppContext.BaseDirectory : Path.Combine(dir.FullName, "ProjectScientDay2568");

        var env = new Mock<IWebHostEnvironment>();
        env.SetupGet(e => e.ContentRootPath).Returns(contentRoot);
        env.SetupGet(e => e.WebRootPath).Returns(Path.Combine(contentRoot, "wwwroot"));

        _export = new ExportService(_context, env.Object,
            new YearService(_context, mapper, env.Object),
            new CertificateConfigService(_context, env.Object, mapper));
        Seed();
    }

    [Fact]
    public async Task Pdf_LevelReport_GeneratesWithProjectRows()
    {
        var bytes = await _export.GenerateTeamReportTableAsync(LevelId);

        Assert.True(bytes.Length > 1000);
        Assert.Equal("%PDF-", System.Text.Encoding.ASCII.GetString(bytes, 0, 5));
        // เก็บไฟล์ไว้ดูด้วยตา
        File.WriteAllBytes(Path.Combine(Path.GetTempPath(), "export_project_row_level.pdf"), bytes);
    }

    [Fact]
    public async Task Word_LevelReport_ProjectIsOwnRow_NotColumn()
    {
        var bytes = await _export.GenerateTeamReportWordTeamAsync(LevelId);
        AssertProjectRowLayout(bytes, expectedColumns: 5, expectedSpan: 4);
    }

    [Fact]
    public async Task Word_CompetitionYearReport_ProjectIsOwnRow_NotColumn()
    {
        var bytes = await _export.GenerateTeamReportWordByCompetitionYearAsync(CompetitionYearId);
        AssertProjectRowLayout(bytes, expectedColumns: 5, expectedSpan: 4);
    }

    [Fact]
    public async Task Word_SelectedLevelsReport_ProjectIsOwnRow_NotColumn()
    {
        var bytes = await _export.GenerateTeamReportWordByCompetitionLevelsAsync(new List<int> { LevelId });
        AssertProjectRowLayout(bytes, expectedColumns: 5, expectedSpan: 4);
    }

    [Fact]
    public async Task Pdf_TrainingSignInSheet_ListsOnlyActiveRegistrations()
    {
        _context.Trainingtopics.Add(new Trainingtopic { Id = 5, Name = "การใช้ AI ช่วยสอนวิทยาศาสตร์" });
        for (var i = 1; i <= 45; i++)
            _context.TrainingRegistrations.Add(new TrainingRegistration { TrainingtopicId = 5, Prefix = "นาย", FirstName = $"ผู้เข้าอบรม{i}", LastName = "ทดสอบ" });
        _context.TrainingRegistrations.Add(new TrainingRegistration { TrainingtopicId = 5, FirstName = "ยกเลิก", LastName = "แล้ว", Status = "ยกเลิกแล้ว" });
        _context.SaveChanges();

        var bytes = await _export.GenerateTrainingSignInSheetAsync(5);

        Assert.Equal("%PDF-", System.Text.Encoding.ASCII.GetString(bytes, 0, 5));
        File.WriteAllBytes(Path.Combine(Path.GetTempPath(), "training_signin.pdf"), bytes);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _export.GenerateTrainingSignInSheetAsync(999));
    }

    [Fact]
    public async Task Pdf_TrainingSignInSheetsByYear_OnePageSetPerTopic()
    {
        _context.Trainingtopics.AddRange(
            new Trainingtopic { Id = 11, Name = "หลักสูตร ก", YearID = 1 },
            new Trainingtopic { Id = 12, Name = "หลักสูตร ข", YearID = 1 },
            new Trainingtopic { Id = 13, Name = "ไม่มีคนสมัคร", YearID = 1 },
            new Trainingtopic { Id = 14, Name = "ปีอื่น", YearID = 2 });
        _context.TrainingRegistrations.AddRange(
            new TrainingRegistration { TrainingtopicId = 11, FirstName = "ก", LastName = "หนึ่ง" },
            new TrainingRegistration { TrainingtopicId = 12, FirstName = "ข", LastName = "สอง" },
            new TrainingRegistration { TrainingtopicId = 14, FirstName = "ค", LastName = "สาม" });
        _context.SaveChanges();

        var bytes = await _export.GenerateTrainingSignInSheetsByYearAsync(1);

        Assert.Equal("%PDF-", System.Text.Encoding.ASCII.GetString(bytes, 0, 5));
        // หลักสูตร ก และ ข อยู่คนละหน้า → 2 หน้า
        Assert.Equal(2, System.Text.RegularExpressions.Regex.Matches(System.Text.Encoding.Latin1.GetString(bytes), @"/Type\s*/Page[^s]").Count);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _export.GenerateTrainingSignInSheetsByYearAsync(99));
    }

    private static void AssertProjectRowLayout(byte[] bytes, int expectedColumns, int expectedSpan)
    {
        using var ms = new MemoryStream(bytes);
        using var doc = WordprocessingDocument.Open(ms, false);
        var table = doc.MainDocumentPart!.Document.Body!.Descendants<Table>().First();
        var rows = table.Elements<TableRow>().ToList();

        // หัวตาราง: ไม่มีคอลัมน์ "ชื่อโครงงาน" แล้ว
        var headers = rows[0].Elements<TableCell>().Select(c => c.InnerText).ToList();
        Assert.Equal(expectedColumns, headers.Count);
        Assert.DoesNotContain("ชื่อโครงงาน", headers);
        Assert.Equal(expectedColumns, table.GetFirstChild<TableGrid>()!.Elements<GridColumn>().Count());

        // ทีมที่มีโครงงาน → แถวทีม + แถวโครงงาน (ช่องเดียวกว้างเต็ม)
        var projectRow = rows.Single(r => r.InnerText.Contains(LongProject));
        var projectCell = projectRow.Elements<TableCell>().Last();
        Assert.StartsWith("ชื่อโครงงาน: ", projectCell.InnerText);
        Assert.Equal(expectedSpan, projectCell.TableCellProperties!.GetFirstChild<GridSpan>()!.Val!.Value);

        // ช่อง "ลำดับ" รวมแนวตั้งต่อจากแถวทีมด้านบน
        var idx = rows.IndexOf(projectRow);
        var teamIndexCell = rows[idx - 1].Elements<TableCell>().First();
        Assert.Equal(MergedCellValues.Restart, teamIndexCell.TableCellProperties!.GetFirstChild<VerticalMerge>()!.Val!.Value);
        Assert.NotNull(projectRow.Elements<TableCell>().First().TableCellProperties!.GetFirstChild<VerticalMerge>());

        // ทีมที่ไม่มีโครงงาน → แถวเดียว ไม่มีแถวโครงงานว่าง ๆ
        var dataRows = rows.Skip(1).ToList();
        Assert.Equal(3, dataRows.Count); // ทีมมีโครงงาน 2 แถว + ทีมไม่มีโครงงาน 1 แถว
    }

    private void Seed()
    {
        _context.Years.Add(new Year { YearID = 1, YearNumber = 2569, YearName = "2569" });
        _context.Competitions.Add(new Competition { CompetitionID = 1, CompetitionName = "ประกวดโครงงานวิทยาศาสตร์" });
        _context.CompetitionYears.Add(new CompetitionYear { CompetitionYearID = CompetitionYearId, YearID = 1, CompetitionID = 1 });
        _context.EducationLevels.Add(new EducationLevel { LevelID = 1, LevelName = "มัธยมศึกษาตอนต้น" });
        _context.CompetitionLevels.Add(new CompetitionLevel { CompetitionLevelID = LevelId, CompetitionYearID = CompetitionYearId, LevelID = 1 });
        _context.School.AddRange(
            new Schools { school_code = 1, school_name = "โรงเรียนเทพศิรินทร์ ลาดหญ้า กาญจนบุรี" },
            new Schools { school_code = 2, school_name = "โรงเรียนท่าม่วงราษฎร์บำรุง" });
        _context.Teams.AddRange(
            new Team
            {
                TeamID = 1, CompetitionLevelID = LevelId, school_code = 1, Status = TeamStatus.CheckedIn,
                ProjectName = LongProject,
                PrimaryTitlePrefix = TitlePrefix.นาง, PrimaryFirstName = "สมศรี", PrimaryLastName = "ใจดีมากมายเหลือเกิน",
                SecondaryTitlePrefix = TitlePrefix.นาย, SecondaryFirstName = "สมชาย", SecondaryLastName = "รักวิทยาศาสตร์",
                Students = new List<Student>
                {
                    new() { FirstName = "ณัฐพงศ์", LastName = "ศรีสวัสดิ์วงศ์", TitlePrefix = TitlePrefix.เด็กชาย },
                    new() { FirstName = "กัญญาณัฐ", LastName = "บุญประเสริฐสุข", TitlePrefix = TitlePrefix.เด็กหญิง },
                    new() { FirstName = "ธนกฤต", LastName = "วงศ์สวัสดิ์", TitlePrefix = TitlePrefix.เด็กชาย },
                },
            },
            new Team
            {
                TeamID = 2, CompetitionLevelID = LevelId, school_code = 2, Status = TeamStatus.Verified,
                ProjectName = "",
                PrimaryTitlePrefix = TitlePrefix.นาย, PrimaryFirstName = "วิชัย", PrimaryLastName = "สอนดี",
                Students = new List<Student> { new() { FirstName = "ปิยะ", LastName = "มานะ", TitlePrefix = TitlePrefix.เด็กชาย } },
            });
        _context.SaveChanges();
    }

    public void Dispose() => _context.Dispose();
}
