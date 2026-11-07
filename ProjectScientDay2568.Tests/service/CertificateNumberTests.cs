using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ProjectScientDay2568.Data;
using ProjectScientDay2568.Mappings;
using ProjectScientDay2568.Models;
using ProjectScientDay2568.Service;
using static ProjectScientDay2568.Utility.SD;

namespace ProjectScientDay2568.Tests.Services;

/// <summary>
/// การรันเลขเกียรติบัตร (GenerateTeamCertificatesNumberByLevel)
/// รูปแบบเลข: "{เลขรัน}/{ปี}" เช่น "50/2569"
/// ปัจจัยที่มีผล: StartNumber ของปี, เลขที่ออกไปแล้วในปีเดียวกัน (ทุกกิจกรรม/ทุกระดับ),
/// สถานะทีม (เฉพาะเช็คอินแล้ว), ลำดับรางวัล, การกดซ้ำ, การ rebuild และทีมที่เพิ่ม/หายไปภายหลัง
/// หมายเหตุ: InMemory ไม่รองรับ transaction จริง จึงทดสอบกรณีกดพร้อมกันหลายคนไม่ได้ในชุดนี้
/// </summary>
public class CertificateNumberTests : IDisposable
{
    private const int Year2569 = 1;
    private const int Year2568 = 2;
    private const int LevelA = 10;     // ปี 2569 กิจกรรม A
    private const int LevelB = 11;     // ปี 2569 กิจกรรม B (ปีเดียวกัน คนละระดับ)
    private const int LevelOld = 20;   // ปี 2568
    private const int LevelNoConfig = 30; // ปี 2570 ไม่ได้ตั้งค่าเกียรติบัตร

    // RewardTypeID: น้อย = รางวัลสูง
    private const int Gold = 1;
    private const int Silver = 2;
    private const int Bronze = 3;

    private readonly ApplicationDbContext _context;
    private readonly CompetitionlevelService _service;
    private int _nextTeamId = 100;

    public CertificateNumberTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"CertificateNumberTests_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _context = new ApplicationDbContext(options);
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
        _service = new CompetitionlevelService(_context, mapper);
        SeedData();
    }

    // ---------- พื้นฐาน ----------

    [Fact]
    public async Task FirstRun_StartsAtStartNumber_WithYearSuffix()
    {
        AddTeam(LevelA, Gold);
        AddTeam(LevelA, Silver);

        var result = await _service.GenerateTeamCertificatesNumberByLevel(LevelA);

        Assert.Equal(new[] { "50/2569", "51/2569" }, result.Select(r => r.CertificateNumber));
    }

    [Fact]
    public async Task Numbers_IgnoreReward_OrderedByTeam()
    {
        var bronze = AddTeam(LevelA, Bronze);
        var noReward = AddTeam(LevelA, null);
        var gold = AddTeam(LevelA, Gold);

        await _service.GenerateTeamCertificatesNumberByLevel(LevelA);

        Assert.Equal("50/2569", NumberOf(bronze));
        Assert.Equal("51/2569", NumberOf(noReward)); // ไม่มีรางวัลก็ได้เลข
        Assert.Equal("52/2569", NumberOf(gold));
    }

    [Theory]
    [InlineData(TeamStatus.Registered)]
    [InlineData(TeamStatus.Verified)]
    [InlineData(TeamStatus.ProjectRevision)]
    [InlineData(TeamStatus.Disqualified)]
    public async Task OnlyCheckedInTeams_GetNumbers(TeamStatus otherStatus)
    {
        var checkedIn = AddTeam(LevelA, Gold);
        var other = AddTeam(LevelA, Silver, otherStatus);

        var result = await _service.GenerateTeamCertificatesNumberByLevel(LevelA);

        Assert.Single(result);
        Assert.Equal("50/2569", NumberOf(checkedIn));
        Assert.Null(NumberOf(other));
    }

    [Fact]
    public async Task NoCheckedInTeams_ReturnsEmpty_AndDoesNotMarkLevelGenerated()
    {
        AddTeam(LevelA, Gold, TeamStatus.Verified);

        var result = await _service.GenerateTeamCertificatesNumberByLevel(LevelA);

        Assert.Empty(result);
        Assert.Empty(_context.TeamCertificates);
        Assert.False(_context.CompetitionLevels.Single(l => l.CompetitionLevelID == LevelA).IsCertificateNumberGenerated);
    }

    [Fact]
    public async Task NoCertificateConfig_Throws()
    {
        AddTeam(LevelNoConfig, Gold);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.GenerateTeamCertificatesNumberByLevel(LevelNoConfig));
        Assert.Equal("ยังไม่ได้ตั้งค่าเกียรติบัตรสำหรับปีนี้", ex.Message);
    }

    [Fact]
    public async Task UnknownLevel_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.GenerateTeamCertificatesNumberByLevel(9999));
    }

    [Fact]
    public async Task CertificateName_ComesFromConfig_Trimmed()
    {
        AddTeam(LevelA, Gold);

        var result = await _service.GenerateTeamCertificatesNumberByLevel(LevelA);

        Assert.Equal("กจ", result.Single().CertificateName);
        Assert.True(_context.CompetitionLevels.Single(l => l.CompetitionLevelID == LevelA).IsCertificateNumberGenerated);
    }

    // ---------- ต่อเลขข้ามกิจกรรมในปีเดียวกัน ----------

    [Fact]
    public async Task SecondLevelInSameYear_ContinuesAfterFirstLevel()
    {
        AddTeam(LevelA, Gold);
        AddTeam(LevelA, Silver);
        var b1 = AddTeam(LevelB, Gold);

        await _service.GenerateTeamCertificatesNumberByLevel(LevelA);
        await _service.GenerateTeamCertificatesNumberByLevel(LevelB);

        Assert.Equal("52/2569", NumberOf(b1));
    }

    [Fact]
    public async Task OtherYearNumbers_DoNotAffectThisYear()
    {
        // ปี 2568 ออกไปถึงเลข 500 แล้ว
        var old = AddTeam(LevelOld, Gold);
        AddCertificate(old, "500/2568");
        var a1 = AddTeam(LevelA, Gold);

        await _service.GenerateTeamCertificatesNumberByLevel(LevelA);

        Assert.Equal("50/2569", NumberOf(a1));
    }

    [Fact]
    public async Task ExistingHigherThanStartNumber_ContinuesFromMaxPlusOne()
    {
        // กิจกรรม B ออกไปถึง 120 แล้ว (เช่น นำเข้าข้อมูลเก่า/StartNumber เคยตั้งสูงกว่า)
        var b = AddTeam(LevelB, Gold);
        AddCertificate(b, "120/2569");
        var a1 = AddTeam(LevelA, Gold);

        await _service.GenerateTeamCertificatesNumberByLevel(LevelA);

        Assert.Equal("121/2569", NumberOf(a1));
    }

    [Fact]
    public async Task StartNumberHigherThanExisting_UsesStartNumber()
    {
        // แอดมินตั้ง StartNumber ใหม่ให้สูงกว่าเลขที่ออกไปแล้ว
        var b = AddTeam(LevelB, Gold);
        AddCertificate(b, "60/2569");
        _context.CertificateConfigs.Single(c => c.YearID == Year2569).StartNumber = 200;
        _context.SaveChanges();
        var a1 = AddTeam(LevelA, Gold);

        await _service.GenerateTeamCertificatesNumberByLevel(LevelA);

        Assert.Equal("200/2569", NumberOf(a1));
    }

    [Theory]
    [InlineData("ไม่มีเลข")]
    [InlineData("/2569")]
    [InlineData("abc/2569")]
    [InlineData("")]
    public async Task MalformedExistingNumbers_AreIgnored(string malformed)
    {
        var b = AddTeam(LevelB, Gold);
        AddCertificate(b, malformed);
        var a1 = AddTeam(LevelA, Gold);

        await _service.GenerateTeamCertificatesNumberByLevel(LevelA);

        Assert.Equal("50/2569", NumberOf(a1));
    }

    // ---------- กดซ้ำ / rebuild ----------

    [Fact]
    public async Task CallingTwice_IsIdempotent_SameNumbers_NoDuplicates()
    {
        AddTeam(LevelA, Gold);
        AddTeam(LevelA, Silver);

        var first = await _service.GenerateTeamCertificatesNumberByLevel(LevelA);
        var second = await _service.GenerateTeamCertificatesNumberByLevel(LevelA);

        Assert.Equal(first.Select(x => x.CertificateNumber).OrderBy(x => x), second.Select(x => x.CertificateNumber).OrderBy(x => x));
        Assert.Equal(2, _context.TeamCertificates.Count());
    }

    [Fact]
    public async Task ForceRebuild_SameTeams_KeepsSameNumberRange()
    {
        AddTeam(LevelA, Gold);
        AddTeam(LevelA, Silver);
        AddTeam(LevelB, Gold);
        await _service.GenerateTeamCertificatesNumberByLevel(LevelA); // 50, 51
        await _service.GenerateTeamCertificatesNumberByLevel(LevelB); // 52

        await _service.GenerateTeamCertificatesNumberByLevel(LevelA, forceRebuild: true);

        Assert.Equal(new[] { "50/2569", "51/2569", "52/2569" }, AllNumbersInYear());
    }

    [Fact]
    public async Task RewardChangedLater_NumbersStayTheSame()
    {
        var t1 = AddTeam(LevelA, Gold);
        var t2 = AddTeam(LevelA, Silver);
        await _service.GenerateTeamCertificatesNumberByLevel(LevelA); // t1=50, t2=51

        SetReward(t1, Silver);
        SetReward(t2, Gold);
        await _service.GenerateTeamCertificatesNumberByLevel(LevelA);
        await _service.GenerateTeamCertificatesNumberByLevel(LevelA, forceRebuild: true);

        Assert.Equal("50/2569", NumberOf(t1));
        Assert.Equal("51/2569", NumberOf(t2));
    }

    // ---------- ทีมเปลี่ยนหลังออกเลขแล้ว (กรณีที่น่าสงสัย) ----------

    [Fact]
    public async Task TeamAddedLater_ExistingTeamsKeepTheirNumbers()
    {
        // ออกเลขระดับ A แล้ว (และอาจพิมพ์เกียรติบัตรไปแล้ว)
        var t1 = AddTeam(LevelA, Gold);
        var t2 = AddTeam(LevelA, Silver);
        await _service.GenerateTeamCertificatesNumberByLevel(LevelA); // 50, 51

        // มีทีมเช็คอินเพิ่มภายหลัง แล้วกดออกเลขอีกครั้ง
        var late = AddTeam(LevelA, Bronze);
        await _service.GenerateTeamCertificatesNumberByLevel(LevelA);

        // ทีมเดิมควรได้เลขเดิม (ใบที่พิมพ์ไปแล้วจะได้ไม่ผิด) ทีมใหม่ได้เลขถัดไป
        Assert.Equal("50/2569", NumberOf(t1));
        Assert.Equal("51/2569", NumberOf(t2));
        Assert.Equal("52/2569", NumberOf(late));
    }

    [Fact]
    public async Task TeamAddedLater_DoesNotLeaveGapsOrDuplicatesInYear()
    {
        AddTeam(LevelA, Gold);
        AddTeam(LevelA, Silver);
        await _service.GenerateTeamCertificatesNumberByLevel(LevelA);
        AddTeam(LevelA, Bronze);
        await _service.GenerateTeamCertificatesNumberByLevel(LevelA);

        var numbers = AllNumbersInYear();
        Assert.Equal(numbers.Distinct().Count(), numbers.Count);
        Assert.Equal(new[] { "50/2569", "51/2569", "52/2569" }, numbers);
    }

    [Fact]
    public async Task TeamDisqualifiedAfterNumbering_IsNotGivenAnotherTeamsNumber()
    {
        var t1 = AddTeam(LevelA, Gold);
        var t2 = AddTeam(LevelA, Silver);
        await _service.GenerateTeamCertificatesNumberByLevel(LevelA); // 50, 51

        SetStatus(t1, TeamStatus.Disqualified);
        await _service.GenerateTeamCertificatesNumberByLevel(LevelA);

        // ห้ามมีเลขซ้ำในปี ไม่ว่าจะจัดการทีมที่ถูกตัดสิทธิ์อย่างไร
        var numbers = AllNumbersInYear();
        Assert.Equal(numbers.Distinct().Count(), numbers.Count);
        Assert.NotNull(NumberOf(t2));
    }

    [Fact]
    public async Task TeamAddedLater_AfterOtherLevelNumbered_GetsNextNumberOfYear()
    {
        var a1 = AddTeam(LevelA, Gold);
        var b1 = AddTeam(LevelB, Gold);
        await _service.GenerateTeamCertificatesNumberByLevel(LevelA); // a1 = 50
        await _service.GenerateTeamCertificatesNumberByLevel(LevelB); // b1 = 51

        var late = AddTeam(LevelA, Gold);
        await _service.GenerateTeamCertificatesNumberByLevel(LevelA);

        Assert.Equal("50/2569", NumberOf(a1));
        Assert.Equal("51/2569", NumberOf(b1));
        Assert.Equal("52/2569", NumberOf(late)); // ต่อท้ายปี ไม่ชนกับกิจกรรม B
    }

    [Fact]
    public async Task ForceRebuild_WithTeamAddedLater_KeepsOldNumbers_AndAppendsNewTeam()
    {
        var t1 = AddTeam(LevelA, Silver);
        var t2 = AddTeam(LevelA, Bronze);
        var b1 = AddTeam(LevelB, Gold);
        await _service.GenerateTeamCertificatesNumberByLevel(LevelA); // t1=50, t2=51
        await _service.GenerateTeamCertificatesNumberByLevel(LevelB); // b1=52

        var late = AddTeam(LevelA, Gold);
        await _service.GenerateTeamCertificatesNumberByLevel(LevelA, forceRebuild: true);

        Assert.Equal("50/2569", NumberOf(t1));
        Assert.Equal("51/2569", NumberOf(t2));
        Assert.Equal("52/2569", NumberOf(b1)); // กิจกรรมอื่นไม่ถูกแตะ
        Assert.Equal("53/2569", NumberOf(late));
    }

    [Fact]
    public async Task Result_ContainsAllTeamsInLevel()
    {
        AddTeam(LevelA, Gold);
        await _service.GenerateTeamCertificatesNumberByLevel(LevelA);
        AddTeam(LevelA, Silver);

        var result = await _service.GenerateTeamCertificatesNumberByLevel(LevelA);

        Assert.Equal(new[] { "50/2569", "51/2569" }, result.Select(r => r.CertificateNumber));
    }

    // ---------- ปุ่ม "ปิดกิจกรรม" ทั้งปี (YearService.GenerateTeamCertificatesNumber) ----------

    private YearService YearService() =>
        new(_context, new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper(), null!);

    [Fact]
    public async Task Year_NumbersEveryCheckedInTeam_AllActivities_InActivityLevelTeamOrder()
    {
        var b1 = AddTeam(LevelB, Gold);
        var a1 = AddTeam(LevelA, Bronze);
        var a2 = AddTeam(LevelA, null);
        var notCheckedIn = AddTeam(LevelA, Gold, TeamStatus.Verified);
        var otherYear = AddTeam(LevelOld, Gold);

        var result = await YearService().GenerateTeamCertificatesNumber(Year2569);

        Assert.Equal(3, result.Count);
        Assert.Equal("50/2569", NumberOf(a1)); // กิจกรรม A ก่อน
        Assert.Equal("51/2569", NumberOf(a2));
        Assert.Equal("52/2569", NumberOf(b1)); // แล้วกิจกรรม B
        Assert.Null(NumberOf(notCheckedIn));
        Assert.Null(NumberOf(otherYear));
        Assert.True(_context.CompetitionLevels.Where(l => l.CompetitionLevelID == LevelA || l.CompetitionLevelID == LevelB)
            .All(l => l.IsCertificateNumberGenerated));
    }

    [Fact]
    public async Task Year_PressedAgain_KeepsAllNumbers_NoDuplicates()
    {
        AddTeam(LevelA, Gold);
        AddTeam(LevelB, Gold);
        await YearService().GenerateTeamCertificatesNumber(Year2569);
        var before = AllNumbersInYear();

        await YearService().GenerateTeamCertificatesNumber(Year2569);

        Assert.Equal(before, AllNumbersInYear());
    }

    [Fact]
    public async Task Year_AfterLevelAlreadyNumbered_KeepsLevelNumbers_AndNumbersTheRest()
    {
        var a1 = AddTeam(LevelA, Gold);
        await _service.GenerateTeamCertificatesNumberByLevel(LevelA); // a1 = 50
        var b1 = AddTeam(LevelB, Gold);
        var a2 = AddTeam(LevelA, Gold);

        await YearService().GenerateTeamCertificatesNumber(Year2569);

        Assert.Equal("50/2569", NumberOf(a1)); // เลขเดิมไม่ถูกลบ/เปลี่ยน
        Assert.Equal("51/2569", NumberOf(a2));
        Assert.Equal("52/2569", NumberOf(b1));
        Assert.Equal(new[] { "50/2569", "51/2569", "52/2569" }, AllNumbersInYear());
    }

    [Fact]
    public async Task Year_TeamDisqualifiedAfterNumbering_KeepsItsNumber_NotReused()
    {
        var a1 = AddTeam(LevelA, Gold);
        await YearService().GenerateTeamCertificatesNumber(Year2569); // a1 = 50
        SetStatus(a1, TeamStatus.Disqualified);
        var a2 = AddTeam(LevelA, Gold);

        await YearService().GenerateTeamCertificatesNumber(Year2569);

        Assert.Equal("50/2569", NumberOf(a1));
        Assert.Equal("51/2569", NumberOf(a2));
    }

    [Fact]
    public async Task Year_NoConfig_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => YearService().GenerateTeamCertificatesNumber(3));
    }

    // ---------- ก่อนดาวน์โหลดเกียรติบัตร (EnsureCertificateNumberForDownloadAsync) ----------

    [Fact]
    public async Task Download_BeforeClosing_IsBlocked()
    {
        var a1 = AddTeam(LevelA, Gold);

        var ex = await Assert.ThrowsAsync<CertificateNotReadyException>(() =>
            _service.EnsureCertificateNumberForDownloadAsync(a1));
        Assert.Contains("ปิดการแข่งขัน", ex.Message);
        Assert.Null(NumberOf(a1)); // ไม่แอบออกเลขให้ก่อนปิด
    }

    [Fact]
    public async Task Download_AfterClosing_TeamWithNumber_Passes_NumberUnchanged()
    {
        var a1 = AddTeam(LevelA, Gold);
        await _service.GenerateTeamCertificatesNumberByLevel(LevelA);

        await _service.EnsureCertificateNumberForDownloadAsync(a1);

        Assert.Equal("50/2569", NumberOf(a1));
    }

    [Fact]
    public async Task Download_TeamCheckedInAfterClosing_GetsNextNumberAutomatically()
    {
        AddTeam(LevelA, Gold);
        await YearService().GenerateTeamCertificatesNumber(Year2569); // 50, ระดับ A ปิดแล้ว
        AddTeam(LevelB, Gold);
        await _service.GenerateTeamCertificatesNumberByLevel(LevelB); // 51
        var late = AddTeam(LevelA, Gold); // เช็คอินหลังปิด

        await _service.EnsureCertificateNumberForDownloadAsync(late);

        Assert.Equal("52/2569", NumberOf(late));
    }

    [Fact]
    public async Task Download_NotCheckedIn_IsBlocked()
    {
        AddTeam(LevelA, Gold);
        await _service.GenerateTeamCertificatesNumberByLevel(LevelA);
        var verified = AddTeam(LevelA, Gold, TeamStatus.Verified);

        await Assert.ThrowsAsync<CertificateNotReadyException>(() =>
            _service.EnsureCertificateNumberForDownloadAsync(verified));
        Assert.Null(NumberOf(verified));
    }

    [Fact]
    public async Task Download_DisqualifiedAfterNumbering_StillHasItsNumber()
    {
        var a1 = AddTeam(LevelA, Gold);
        await _service.GenerateTeamCertificatesNumberByLevel(LevelA);
        SetStatus(a1, TeamStatus.Disqualified);

        await _service.EnsureCertificateNumberForDownloadAsync(a1); // มีเลขแล้ว ไม่ throw

        Assert.Equal("50/2569", NumberOf(a1));
    }

    [Fact]
    public async Task Download_UnknownTeam_NotFound()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.EnsureCertificateNumberForDownloadAsync(99999));
    }

    // ---------- helpers ----------

    private int AddTeam(int levelId, int? rewardTypeId, TeamStatus status = TeamStatus.CheckedIn)
    {
        var id = _nextTeamId++;
        _context.Teams.Add(new Team
        {
            TeamID = id,
            CompetitionLevelID = levelId,
            RewardTypeID = rewardTypeId,
            Status = status,
            PrimaryFirstName = $"ทีม{id}",
        });
        _context.SaveChanges();
        return id;
    }

    private void AddCertificate(int teamId, string number)
    {
        _context.TeamCertificates.Add(new TeamCertificate { TeamID = teamId, CertificateNumber = number, CertificateName = "กจ" });
        _context.SaveChanges();
    }

    private void SetReward(int teamId, int rewardTypeId)
    {
        _context.Teams.Single(t => t.TeamID == teamId).RewardTypeID = rewardTypeId;
        _context.SaveChanges();
    }

    private void SetStatus(int teamId, TeamStatus status)
    {
        _context.Teams.Single(t => t.TeamID == teamId).Status = status;
        _context.SaveChanges();
    }

    private string? NumberOf(int teamId)
    {
        _context.ChangeTracker.Clear();
        return _context.TeamCertificates.Where(c => c.TeamID == teamId).Select(c => c.CertificateNumber).SingleOrDefault();
    }

    private List<string> AllNumbersInYear()
    {
        _context.ChangeTracker.Clear();
        return _context.TeamCertificates
            .Where(c => c.CertificateNumber!.EndsWith("/2569"))
            .Select(c => c.CertificateNumber!)
            .AsEnumerable()
            .OrderBy(n => int.Parse(n.Split('/')[0]))
            .ToList();
    }

    private void SeedData()
    {
        _context.Years.AddRange(
            new Year { YearID = Year2569, YearNumber = 2569, YearName = "2569" },
            new Year { YearID = Year2568, YearNumber = 2568, YearName = "2568" },
            new Year { YearID = 3, YearNumber = 2570, YearName = "2570" });
        _context.Competitions.AddRange(
            new Competition { CompetitionID = 1, CompetitionName = "กิจกรรม A" },
            new Competition { CompetitionID = 2, CompetitionName = "กิจกรรม B" });
        _context.CompetitionYears.AddRange(
            new CompetitionYear { CompetitionYearID = 1, YearID = Year2569, CompetitionID = 1 },
            new CompetitionYear { CompetitionYearID = 2, YearID = Year2569, CompetitionID = 2 },
            new CompetitionYear { CompetitionYearID = 3, YearID = Year2568, CompetitionID = 1 },
            new CompetitionYear { CompetitionYearID = 4, YearID = 3, CompetitionID = 1 });
        _context.CompetitionLevels.AddRange(
            new CompetitionLevel { CompetitionLevelID = LevelA, CompetitionYearID = 1, LevelID = 1 },
            new CompetitionLevel { CompetitionLevelID = LevelB, CompetitionYearID = 2, LevelID = 1 },
            new CompetitionLevel { CompetitionLevelID = LevelOld, CompetitionYearID = 3, LevelID = 1 },
            new CompetitionLevel { CompetitionLevelID = LevelNoConfig, CompetitionYearID = 4, LevelID = 1 });
        _context.CertificateConfigs.AddRange(
            new CertificateConfig { YearID = Year2569, StartNumber = 50, CertificateName = " กจ " },
            new CertificateConfig { YearID = Year2568, StartNumber = 1, CertificateName = "กจ" });
        _context.SaveChanges();
    }

    public void Dispose() => _context.Dispose();
}
