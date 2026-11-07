using System.Security.Claims;
using System.Text;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using ProjectScientDay2568.Data;
using ProjectScientDay2568.Mappings;
using ProjectScientDay2568.Models;
using ProjectScientDay2568.Models.Dtos.Team;
using ProjectScientDay2568.Models.YourNamespace.Models;
using ProjectScientDay2568.Service;

namespace ProjectScientDay2568.Tests.Services;

/// <summary>
/// การกดบันทึกทีม (CreateTeamAsync) โดยเฉพาะกิจกรรมที่ต้องส่งโครงงาน
/// เคยเกิดเหตุสมัครได้โดยไม่แนบโครงงาน จึงต้องกันไว้ที่ server
/// </summary>
public class TeamServiceCreateTests : IDisposable
{
    private const int ProjectLevelId = 10;   // กิจกรรมที่ต้องส่งโครงงาน
    private const int NoProjectLevelId = 20; // กิจกรรมที่ไม่ต้องส่งโครงงาน
    private const int ClosedLevelId = 30;    // กิจกรรมที่ปิดรับสมัครแล้ว
    private const int SchoolCode = 1001;

    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly List<string> _createdFiles = new();

    public TeamServiceCreateTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"TeamServiceCreateTests_{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationDbContext(options);
        _mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();

        SeedData();
    }

    // ---------- กิจกรรมที่ต้องส่งโครงงาน ----------

    [Fact]
    public async Task Create_ProjectRequired_WithoutFile_ShouldThrow_AndNotSaveTeam()
    {
        var service = CreateService(role: "user");
        var dto = CreateDto(ProjectLevelId, projectName: "โครงงานทดสอบ");

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateTeamAsync(dto, "user-1", file: null, projectName: dto.ProjectName));

        Assert.Equal("กิจกรรมนี้ต้องแนบไฟล์โครงงาน", ex.Message);
        Assert.Empty(_context.Teams);
    }

    [Fact]
    public async Task Create_ProjectRequired_WithEmptyFile_ShouldThrow_AndNotSaveTeam()
    {
        var service = CreateService(role: "user");
        var dto = CreateDto(ProjectLevelId, projectName: "โครงงานทดสอบ");
        var emptyFile = CreateFile("project.pdf", content: "");

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateTeamAsync(dto, "user-1", emptyFile, dto.ProjectName));

        Assert.Equal("กิจกรรมนี้ต้องแนบไฟล์โครงงาน", ex.Message);
        Assert.Empty(_context.Teams);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_ProjectRequired_WithoutProjectName_ShouldThrow_AndNotSaveTeam(string? projectName)
    {
        var service = CreateService(role: "user");
        var dto = CreateDto(ProjectLevelId, projectName);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateTeamAsync(dto, "user-1", CreateFile("project.pdf"), projectName));

        Assert.Equal("กิจกรรมนี้ต้องระบุชื่อโครงงาน", ex.Message);
        Assert.Empty(_context.Teams);
    }

    [Theory]
    [InlineData("virus.exe")]
    [InlineData("script.js")]
    [InlineData("image.png")]
    [InlineData("noextension")]
    public async Task Create_WithDisallowedFileType_ShouldThrow_AndNotSaveTeam(string fileName)
    {
        var service = CreateService(role: "user");
        var dto = CreateDto(ProjectLevelId, projectName: "โครงงานทดสอบ");

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateTeamAsync(dto, "user-1", CreateFile(fileName), dto.ProjectName));

        Assert.Contains("ไฟล์โครงงานต้องเป็น", ex.Message);
        Assert.Empty(_context.Teams);
    }

    [Theory]
    [InlineData("project.pdf")]
    [InlineData("PROJECT.PDF")]
    [InlineData("project.docx")]
    [InlineData("project.doc")]
    [InlineData("project.zip")]
    [InlineData("project.rar")]
    public async Task Create_ProjectRequired_WithValidFileAndName_ShouldSaveTeamWithProject(string fileName)
    {
        var service = CreateService(role: "user");
        var dto = CreateDto(ProjectLevelId, projectName: "โครงงานทดสอบ");

        await service.CreateTeamAsync(dto, "user-1", CreateFile(fileName), dto.ProjectName);

        var team = Assert.Single(_context.Teams);
        TrackUploadedFile(team.ProjectPDFLink);
        Assert.Equal("โครงงานทดสอบ", team.ProjectName);
        Assert.False(string.IsNullOrWhiteSpace(team.ProjectPDFLink));
        Assert.StartsWith("/uploads/projects/", team.ProjectPDFLink);
        Assert.Equal("user-1", team.UserId);
    }

    // ---------- กิจกรรมที่ไม่ต้องส่งโครงงาน ----------

    [Fact]
    public async Task Create_ProjectNotRequired_WithoutFile_ShouldSaveTeam()
    {
        var service = CreateService(role: "user");
        var dto = CreateDto(NoProjectLevelId, projectName: null);

        await service.CreateTeamAsync(dto, "user-1", file: null, projectName: null);

        var team = Assert.Single(_context.Teams);
        Assert.Null(team.ProjectPDFLink);
    }

    [Fact]
    public async Task Create_ProjectNotRequired_WithDisallowedFileType_ShouldStillThrow()
    {
        var service = CreateService(role: "user");
        var dto = CreateDto(NoProjectLevelId, projectName: null);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateTeamAsync(dto, "user-1", CreateFile("virus.exe"), null));

        Assert.Empty(_context.Teams);
    }

    // ---------- สิทธิ์เจ้าหน้าที่ ----------

    [Theory]
    [InlineData("admin")]
    [InlineData("Staff")]
    public async Task Create_ProjectRequired_AsStaffOrAdmin_WithoutFile_ShouldSaveTeam(string role)
    {
        var service = CreateService(role);
        var dto = CreateDto(ProjectLevelId, projectName: null);

        await service.CreateTeamAsync(dto, "staff-1", file: null, projectName: null);

        Assert.Single(_context.Teams);
    }

    [Fact]
    public async Task Create_AsStaff_WithDisallowedFileType_ShouldStillThrow()
    {
        var service = CreateService(role: "admin");
        var dto = CreateDto(ProjectLevelId, projectName: "โครงงานทดสอบ");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateTeamAsync(dto, "staff-1", CreateFile("virus.exe"), dto.ProjectName));

        Assert.Empty(_context.Teams);
    }

    // ---------- เงื่อนไขอื่นของการบันทึก ----------

    [Fact]
    public async Task Create_AfterRegistrationClosed_ShouldThrow_AndNotSaveTeam()
    {
        var service = CreateService(role: "user");
        var dto = CreateDto(ClosedLevelId, projectName: null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateTeamAsync(dto, "user-1", null, null));

        Assert.Empty(_context.Teams);
    }

    [Fact]
    public async Task Create_WithUnknownSchool_ShouldThrow_AndNotSaveTeam()
    {
        var service = CreateService(role: "user");
        var dto = CreateDto(NoProjectLevelId, projectName: null);
        dto.school_code = 999999;

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateTeamAsync(dto, "user-1", null, null));

        Assert.Empty(_context.Teams);
    }

    [Fact]
    public async Task Create_SameSchoolTwice_ShouldIncreaseOrdinalAndTeamNumber()
    {
        var service = CreateService(role: "user");

        await service.CreateTeamAsync(CreateDto(NoProjectLevelId, null), "user-1", null, null);
        await service.CreateTeamAsync(CreateDto(NoProjectLevelId, null), "user-1", null, null);

        var teams = _context.Teams.OrderBy(t => t.TeamNumberInLevel).ToList();
        Assert.Equal(2, teams.Count);
        Assert.Equal(new int?[] { 0, 1 }, teams.Select(t => t.NameOrdinal).ToArray());
        Assert.Equal(new int?[] { 1, 2 }, teams.Select(t => t.TeamNumberInLevel).ToArray());
    }

    // ---------- helpers ----------

    private TeamService CreateService(string role)
    {
        var claims = new[] { new Claim("id", "user-1"), new Claim("role", role) };
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")),
        };
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(httpContext);

        return new TeamService(_context, _mapper, new UserContextService(accessor.Object));
    }

    private static TeamCreateDto CreateDto(int levelId, string? projectName) => new()
    {
        CompetitionLevelID = levelId,
        school_code = SchoolCode,
        PrimaryTitlePrefix = 0,
        PrimaryFirstName = "สมชาย",
        PrimaryLastName = "ใจดี",
        ProjectName = projectName,
    };

    private static IFormFile CreateFile(string fileName, string content = "%PDF-1.4 test")
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "File", fileName);
    }

    private void TrackUploadedFile(string? link)
    {
        if (string.IsNullOrEmpty(link)) return;
        _createdFiles.Add(Path.Combine("wwwroot", link.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));
    }

    private void SeedData()
    {
        var openYear = new Year { YearID = 1, YearNumber = 2569, YearName = "2569", RegistrationCloseDate = DateTime.Now.AddDays(30) };
        var closedYear = new Year { YearID = 2, YearNumber = 2568, YearName = "2568", RegistrationCloseDate = DateTime.Now.AddDays(-1) };

        var projectComp = new Competition { CompetitionID = 1, CompetitionName = "ประกวดโครงงานวิทยาศาสตร์", RequiresProject = true };
        var plainComp = new Competition { CompetitionID = 2, CompetitionName = "ตอบปัญหาวิทยาศาสตร์", RequiresProject = false };

        _context.Years.AddRange(openYear, closedYear);
        _context.Competitions.AddRange(projectComp, plainComp);
        _context.CompetitionYears.AddRange(
            new CompetitionYear { CompetitionYearID = 1, YearID = 1, CompetitionID = 1 },
            new CompetitionYear { CompetitionYearID = 2, YearID = 1, CompetitionID = 2 },
            new CompetitionYear { CompetitionYearID = 3, YearID = 2, CompetitionID = 2 });
        _context.CompetitionLevels.AddRange(
            new CompetitionLevel { CompetitionLevelID = ProjectLevelId, CompetitionYearID = 1, LevelID = 1 },
            new CompetitionLevel { CompetitionLevelID = NoProjectLevelId, CompetitionYearID = 2, LevelID = 1 },
            new CompetitionLevel { CompetitionLevelID = ClosedLevelId, CompetitionYearID = 3, LevelID = 1 });
        _context.School.Add(new Schools { school_code = SchoolCode, school_name = "โรงเรียนทดสอบ" });

        _context.SaveChanges();
    }

    public void Dispose()
    {
        foreach (var path in _createdFiles.Where(File.Exists))
            File.Delete(path);
        _context.Dispose();
    }
}
