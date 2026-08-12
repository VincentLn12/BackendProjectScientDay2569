using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;
using ProjectScientDay2568.Data;
using ProjectScientDay2568.Hubs;
using ProjectScientDay2568.Models;
using ProjectScientDay2568.Models.YourNamespace.Models;
using ProjectScientDay2568.Models.Dtos.Match;
using ProjectScientDay2568.Service;
using ProjectScientDay2568.Utility;
using MatchEntity = ProjectScientDay2568.Models.Match;

namespace ProjectScientDay2568.Tests.Services;

public class MatchServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    private readonly Mock<IHubContext<MatchHub>> _hubContextMock;
    private readonly Mock<IHubClients> _hubClientsMock;
    private readonly Mock<IClientProxy> _clientProxyMock;
    private readonly Mock<IMapper> _mapperMock;

    private readonly MatchService _service;

    public MatchServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"MatchServiceTests_{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationDbContext(options);

        _hubContextMock = new Mock<IHubContext<MatchHub>>();
        _hubClientsMock = new Mock<IHubClients>();
        _clientProxyMock = new Mock<IClientProxy>();
        _mapperMock = new Mock<IMapper>();

        _hubContextMock
            .Setup(x => x.Clients)
            .Returns(_hubClientsMock.Object);

        _hubClientsMock
            .Setup(x => x.Group(It.IsAny<string>()))
            .Returns(_clientProxyMock.Object);

        _clientProxyMock
            .Setup(x => x.SendCoreAsync(
                It.IsAny<string>(),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _service = new MatchService(
            _context,
            _hubContextMock.Object,
            _mapperMock.Object);
    }

    [Fact]
    public async Task SubmitResultAsync_WhenMatchNotFound_ShouldThrow()
    {
        var action = async () =>
            await _service.SubmitResultAsync(999, 1);

        var exception =
            await Assert.ThrowsAsync<KeyNotFoundException>(action);

        Assert.Equal("Match not found.", exception.Message);
    }

    [Fact]
    public async Task SubmitResultAsync_WhenWinnerTeamNotFound_ShouldThrow()
    {
        var teamA = CreateTeam(1, "ทีม A");
        var teamB = CreateTeam(2, "ทีม B");

        var match = CreateMatch(
            id: 1,
            competitionLevelId: 10,
            round: 1,
            teamA: teamA,
            teamB: teamB);

        await _context.Teams.AddRangeAsync(teamA, teamB);
        await _context.Matches.AddAsync(match);
        await _context.SaveChangesAsync();

        var action = async () =>
            await _service.SubmitResultAsync(match.Id, 999);

        var exception =
            await Assert.ThrowsAsync<KeyNotFoundException>(action);

        Assert.Equal("Winning team not found.", exception.Message);
    }

    [Fact]
    public async Task SubmitResultAsync_WhenWinnerIsNotParticipant_ShouldThrow()
    {
        var teamA = CreateTeam(1, "ทีม A");
        var teamB = CreateTeam(2, "ทีม B");
        var teamC = CreateTeam(3, "ทีม C");

        var match = CreateMatch(
            id: 1,
            competitionLevelId: 10,
            round: 1,
            teamA: teamA,
            teamB: teamB);

        await _context.Teams.AddRangeAsync(teamA, teamB, teamC);
        await _context.Matches.AddAsync(match);
        await _context.SaveChangesAsync();

        var action = async () =>
            await _service.SubmitResultAsync(
                match.Id,
                teamC.TeamID);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(action);

        Assert.Equal(
            "ผู้ชนะจะต้องเป็นหนึ่งในผู้เข้าร่วมการแข่งขัน",
            exception.Message);
    }

    [Fact]
    public async Task SubmitResultAsync_WhenCompletedAndNotEdit_ShouldThrow()
    {
        var teamA = CreateTeam(1, "ทีม A");
        var teamB = CreateTeam(2, "ทีม B");

        var match = CreateMatch(
            id: 1,
            competitionLevelId: 10,
            round: 1,
            teamA: teamA,
            teamB: teamB);

        match.Status = "Completed";
        match.Winner = teamA;

        await _context.Teams.AddRangeAsync(teamA, teamB);
        await _context.Matches.AddAsync(match);
        await _context.SaveChangesAsync();

        var action = async () =>
            await _service.SubmitResultAsync(
                match.Id,
                teamB.TeamID,
                isEdit: false);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(action);

        Assert.Equal("การแข่งขันเสร็จสิ้นแล้ว", exception.Message);
    }

    [Fact]
    public async Task SubmitResultAsync_WithValidWinner_ShouldCompleteMatch()
    {
        var teamA = CreateTeam(1, "ทีม A");
        var teamB = CreateTeam(2, "ทีม B");

        var match = CreateMatch(
            id: 1,
            competitionLevelId: 10,
            round: 1,
            teamA: teamA,
            teamB: teamB);

        await _context.Teams.AddRangeAsync(teamA, teamB);
        await _context.Matches.AddAsync(match);
        await _context.SaveChangesAsync();

        var expectedDto = new MatchDto();

        _mapperMock
            .Setup(x => x.Map<MatchDto>(It.IsAny<MatchEntity>()))
            .Returns(expectedDto);

        var result = await _service.SubmitResultAsync(
            match.Id,
            teamA.TeamID);

        var savedMatch = await _context.Matches
            .Include(x => x.TeamA)
            .Include(x => x.TeamB)
            .Include(x => x.Winner)
            .FirstAsync(x => x.Id == match.Id);

        Assert.Equal("Completed", savedMatch.Status);

        Assert.NotNull(savedMatch.Winner);
        Assert.Equal(teamA.TeamID, savedMatch.Winner!.TeamID);

        Assert.True(savedMatch.TeamA!.IsLuckyDrawWinner);
        Assert.False(savedMatch.TeamB!.IsLuckyDrawWinner);

        Assert.Same(expectedDto, result);

        VerifySignalR(
            groupName: "10",
            expectedMessage: "Match 1 updated.");
    }

    [Fact]
    public async Task GenerateMatchBracketAsync_WhenLevelNotFound_ShouldThrow()
    {
        var action = async () =>
            await _service.GenerateMatchBracketAsync(999);

        var exception =
            await Assert.ThrowsAsync<KeyNotFoundException>(action);

        Assert.Equal("ไม่พบการแข่งขัน.", exception.Message);
    }

    [Fact]
    public async Task GenerateMatchBracketAsync_WhenTeamsLessThanTwo_ShouldThrow()
    {
        var level = CreateCompetitionLevel(
            competitionLevelId: 10,
            teamCount: 1);

        await _context.CompetitionLevels.AddAsync(level);
        await _context.SaveChangesAsync();

        var action = async () =>
            await _service.GenerateMatchBracketAsync(10);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(action);

        Assert.Equal(
            "ต้องมีอย่างน้อย 2 ทีมที่ได้รับการอนุมัติเพื่อสร้างการแข่งขัน.",
            exception.Message);
    }

    [Fact]
    public async Task GenerateMatchBracketAsync_WhenSampleSizeLessThanTwo_ShouldThrow()
    {
        var level = CreateCompetitionLevel(
            competitionLevelId: 10,
            teamCount: 4);

        await _context.CompetitionLevels.AddAsync(level);
        await _context.SaveChangesAsync();

        var action = async () =>
            await _service.GenerateMatchBracketAsync(
                competitionLevelID: 10,
                sampleSize: 1);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(action);

        Assert.Equal(
            "sampleSize ต้องมากกว่าหรือเท่ากับ 2",
            exception.Message);
    }

    [Fact]
    public async Task GenerateMatchBracketAsync_WhenSampleSizeExceedsTeams_ShouldThrow()
    {
        var level = CreateCompetitionLevel(
            competitionLevelId: 10,
            teamCount: 4);

        await _context.CompetitionLevels.AddAsync(level);
        await _context.SaveChangesAsync();

        var action = async () =>
            await _service.GenerateMatchBracketAsync(
                competitionLevelID: 10,
                sampleSize: 5);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(action);

        Assert.Equal(
            "sampleSize มากกว่าจำนวนทีมที่มีอยู่ (4)",
            exception.Message);
    }

    [Fact]
    public async Task GenerateMatchBracketAsync_WithFourTeams_ShouldCreateTwoMatches()
    {
        var level = CreateCompetitionLevel(
            competitionLevelId: 10,
            teamCount: 4);

        await _context.CompetitionLevels.AddAsync(level);
        await _context.SaveChangesAsync();

        _mapperMock
            .Setup(x => x.Map<List<MatchDto>>(
                It.IsAny<List<MatchEntity>>()))
            .Returns(new List<MatchDto>
            {
                new(),
                new()
            });

        var result =
            await _service.GenerateMatchBracketAsync(10);

        var matches = await _context.Matches
            .Where(x => x.CompetitionLevelID == 10)
            .ToListAsync();

        Assert.Equal(2, matches.Count);
        Assert.Equal(2, result.Count);

        Assert.All(matches, match =>
        {
            Assert.Equal(1, match.Round);
            Assert.Equal("Pending", match.Status);
            Assert.NotNull(match.TeamA);
            Assert.NotNull(match.TeamB);
            Assert.Null(match.Winner);
        });

        VerifySignalR(
            groupName: "10",
            expectedMessage: "รอบแรกถูกสร้างแล้ว");
    }

    [Fact]
    public async Task GenerateMatchBracketAsync_WithFiveTeams_ShouldCreateBye()
    {
        var level = CreateCompetitionLevel(
            competitionLevelId: 10,
            teamCount: 5);

        await _context.CompetitionLevels.AddAsync(level);
        await _context.SaveChangesAsync();

        _mapperMock
            .Setup(x => x.Map<List<MatchDto>>(
                It.IsAny<List<MatchEntity>>()))
            .Returns(new List<MatchDto>());

        await _service.GenerateMatchBracketAsync(10);

        var matches = await _context.Matches
            .Include(x => x.TeamA)
            .Include(x => x.TeamB)
            .Include(x => x.Winner)
            .Where(x => x.CompetitionLevelID == 10)
            .ToListAsync();

        Assert.Equal(3, matches.Count);

        var byeMatch = Assert.Single(
            matches.Where(x => x.Status == "Bye"));

        Assert.NotNull(byeMatch.TeamA);
        Assert.Null(byeMatch.TeamB);
        Assert.NotNull(byeMatch.Winner);

        Assert.Equal(
            byeMatch.TeamA!.TeamID,
            byeMatch.Winner!.TeamID);
    }

    [Fact]
    public async Task GenerateNextRoundAsync_WhenCurrentRoundIncomplete_ShouldReturnMessage()
    {
        var teamA = CreateTeam(1, "ทีม A");
        var teamB = CreateTeam(2, "ทีม B");

        var match = CreateMatch(
            id: 1,
            competitionLevelId: 10,
            round: 1,
            teamA: teamA,
            teamB: teamB);

        match.Status = "Pending";

        await _context.Teams.AddRangeAsync(teamA, teamB);
        await _context.Matches.AddAsync(match);
        await _context.SaveChangesAsync();

        var result = await _service.GenerateNextRoundAsync(
            competitionLevelID: 10,
            currentRound: 1);

        Assert.Equal(
            "ไม่สามารถสร้างรอบที่ 2 ได้ เพราะยังมีแมตช์ในรอบที่ 1 ที่ยังไม่จบ",
            result);

        Assert.False(await _context.Matches.AnyAsync(
            x => x.CompetitionLevelID == 10 &&
                 x.Round == 2));
    }

    [Fact]
    public async Task GenerateNextRoundAsync_WhenWinnerLessThanTwo_ShouldReturnMessage()
    {
        var result = await _service.GenerateNextRoundAsync(
            competitionLevelID: 10,
            currentRound: 1);

        Assert.Equal(
            "ไม่สามารถสร้างรอบที่ 2 ได้ เพราะมีผู้ชนะไม่เพียงพอ",
            result);
    }

    [Fact]
    public async Task GenerateNextRoundAsync_WhenOnlyOneWinner_ShouldReturnChampion()
    {
        var teamA = CreateTeam(1, "โรงเรียนผู้ชนะ");
        var teamB = CreateTeam(2, "โรงเรียนคู่แข่ง");

        var match = CreateMatch(
            id: 1,
            competitionLevelId: 10,
            round: 1,
            teamA: teamA,
            teamB: teamB);

        match.Status = "Completed";
        match.Winner = teamA;

        await _context.Teams.AddRangeAsync(teamA, teamB);
        await _context.Matches.AddAsync(match);
        await _context.SaveChangesAsync();

        var result = await _service.GenerateNextRoundAsync(
            competitionLevelID: 10,
            currentRound: 1);

        Assert.Equal(
            "การแข่งขันจบแล้ว! ผู้ชนะคือ โรงเรียนผู้ชนะ",
            result);
    }

    [Fact]
    public async Task GenerateNextRoundAsync_WithFourWinners_ShouldCreateTwoMatches()
    {
        var teams = Enumerable.Range(1, 8)
            .Select(x => CreateTeam(x, $"โรงเรียน {x}"))
            .ToList();

        var matches = new List<MatchEntity>();

        for (var index = 0; index < 8; index += 2)
        {
            matches.Add(new MatchEntity
            {
                Id = index / 2 + 1,
                CompetitionLevelID = 10,
                Round = 1,
                TeamA = teams[index],
                TeamB = teams[index + 1],
                Winner = teams[index],
                Status = "Completed",
                MatchTime = DateTime.UtcNow
            });
        }

        await _context.Teams.AddRangeAsync(teams);
        await _context.Matches.AddRangeAsync(matches);
        await _context.SaveChangesAsync();

        var result = await _service.GenerateNextRoundAsync(
            competitionLevelID: 10,
            currentRound: 1);

        var roundTwoMatches = await _context.Matches
            .Where(x =>
                x.CompetitionLevelID == 10 &&
                x.Round == 2)
            .ToListAsync();

        Assert.Equal(2, roundTwoMatches.Count);

        Assert.All(roundTwoMatches, match =>
        {
            Assert.Equal("Pending", match.Status);
            Assert.NotNull(match.TeamA);
            Assert.NotNull(match.TeamB);
        });

        Assert.Contains("สร้างรอบที่ 2 สำเร็จ", result);

        VerifySignalR(
            groupName: "10",
            expectedMessage: "สร้างรอบที่ 2 สำเร็จ");
    }

    [Fact]
    public async Task CancelRoundAsync_WhenRoundNotFound_ShouldReturnMessage()
    {
        var result = await _service.CancelRoundAsync(
            competitionLevelID: 10,
            roundNumber: 2);

        Assert.Equal(
            "ไม่พบแมตช์ในรอบที่ 2 ขึ้นไปให้ยกเลิก",
            result);
    }

    [Fact]
    public async Task CancelRoundAsync_ShouldDeleteSelectedAndLaterRounds()
    {
        var teamA = CreateTeam(1, "ทีม A");
        var teamB = CreateTeam(2, "ทีม B");

        var roundOne = CreateMatch(
            id: 1,
            competitionLevelId: 10,
            round: 1,
            teamA: teamA,
            teamB: teamB);

        roundOne.Status = "Completed";
        roundOne.Winner = teamA;

        var roundTwo = new MatchEntity
        {
            Id = 2,
            CompetitionLevelID = 10,
            Round = 2,
            TeamA = teamA,
            TeamB = null,
            Winner = teamA,
            Status = "Bye",
            MatchTime = DateTime.UtcNow
        };

        await _context.Teams.AddRangeAsync(teamA, teamB);
        await _context.Matches.AddRangeAsync(roundOne, roundTwo);
        await _context.SaveChangesAsync();

        var result = await _service.CancelRoundAsync(
            competitionLevelID: 10,
            roundNumber: 2);

        var remainingMatches = await _context.Matches
            .Where(x => x.CompetitionLevelID == 10)
            .ToListAsync();

        Assert.Single(remainingMatches);
        Assert.Equal(1, remainingMatches[0].Round);
        Assert.Null(remainingMatches[0].Winner);

        Assert.Contains("ยกเลิกรอบที่ 2 สำเร็จ", result);

        VerifySignalR(
            groupName: "10",
            expectedMessage: "ยกเลิกรอบสำเร็จ");
    }

    private static MatchEntity CreateMatch(
        int id,
        int competitionLevelId,
        int round,
        Team teamA,
        Team? teamB)
    {
        return new MatchEntity
        {
            Id = id,
            CompetitionLevelID = competitionLevelId,
            Round = round,
            TeamA = teamA,
            TeamB = teamB,
            Status = "Pending",
            MatchTime = DateTime.UtcNow
        };
    }

    private static Team CreateTeam(
        int teamId,
        string schoolName)
    {
        return new Team
        {
            TeamID = teamId,
            Status = SD.TeamStatus.CheckedIn,

            schools = new Schools
            {
                school_name = schoolName
            }
        };
    }

    private static CompetitionLevel CreateCompetitionLevel(
        int competitionLevelId,
        int teamCount)
    {
        var teams = Enumerable.Range(1, teamCount)
            .Select(index =>
                CreateTeam(index, $"โรงเรียน {index}"))
            .ToList();

        return new CompetitionLevel
        {
            CompetitionLevelID = competitionLevelId,
            Teams = teams,
            Matches = new List<MatchEntity>()
        };
    }

    private void VerifySignalR(
        string groupName,
        string expectedMessage)
    {
        _hubClientsMock.Verify(
            x => x.Group(groupName),
            Times.Once);

        _clientProxyMock.Verify(
            x => x.SendCoreAsync(
                "ReceiveMatchUpdate",
                It.Is<object?[]>(arguments =>
                    arguments.Length == 1 &&
                    arguments[0] != null &&
                    arguments[0]!.ToString() == expectedMessage),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
