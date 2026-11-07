using System.Text.Json;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.SignalR;
using ProjectScientDay2568.Hubs;
using ProjectScientDay2568.Models.Dtos.Match;
using ProjectScientDay2568.Utility;

namespace ProjectScientDay2568.Service
{
    public class MatchService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<MatchHub> _hubContext;
        private readonly IMapper _mapper;

        public MatchService(
            ApplicationDbContext context,
            IHubContext<MatchHub> hubContext,
            IMapper mapper)
        {
            _context = context;
            _hubContext = hubContext;
            _mapper = mapper;
        }

        /// <summary>
        /// ตรวจสอบและสร้างตาราง MatchDrawLogs โดยอัตโนมัติหากยังไม่มีในฐานข้อมูล
        /// </summary>
        public async Task EnsureTableCreatedAsync()
        {
            try
            {
                var sql = @"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MatchDrawLogs')
                    BEGIN
                        CREATE TABLE [MatchDrawLogs] (
                            [Id] INT IDENTITY(1,1) NOT NULL,
                            [CompetitionLevelID] INT NOT NULL,
                            [Round] INT NOT NULL,
                            [ActionType] NVARCHAR(MAX) NOT NULL DEFAULT '',
                            [Note] NVARCHAR(MAX) NOT NULL DEFAULT '',
                            [MatchesJson] NVARCHAR(MAX) NOT NULL DEFAULT '',
                            [CreatedAt] DATETIME2 NOT NULL,
                            CONSTRAINT [PK_MatchDrawLogs] PRIMARY KEY ([Id]),
                            CONSTRAINT [FK_MatchDrawLogs_CompetitionLevels_CompetitionLevelID] 
                                FOREIGN KEY ([CompetitionLevelID]) REFERENCES [CompetitionLevels] ([CompetitionLevelID]) ON DELETE CASCADE
                        );
                    END;";
                await _context.Database.ExecuteSqlRawAsync(sql);
            }
            catch
            {
                // หากตารางมีอยู่แล้วหรือเกิดข้อผิดพลาด ให้ปล่อยข้ามไปอย่างปลอดภัย
            }
        }

        /// <summary>
        /// ดึงแมตช์ทั้งหมดของ competition level เป็น DTO
        /// </summary>
        public async Task<List<MatchListDto>> GetMatchesByCompetitionLevelIdAsync(int competitionLevelID)
        {
            return await _context.Matches
                .Where(m => m.CompetitionLevelID == competitionLevelID)
                .OrderBy(m => m.Round)
                .ThenBy(m => m.Id)
                .AsNoTracking()
                .Select(m => new MatchListDto
                {
                    Id = m.Id,
                    CompetitionLevelID = m.CompetitionLevelID,
                    Round = m.Round,
                    Status = m.Status,
                    MatchTime = m.MatchTime,
                    TeamA = m.TeamA == null ? null : new MatchTeamDto
                    {
                        TeamID = m.TeamA.TeamID,
                        TeamNumberInLevel = m.TeamA.TeamNumberInLevel,
                        school_name = m.TeamA.schools.school_name
                    },
                    TeamB = m.TeamB == null ? null : new MatchTeamDto
                    {
                        TeamID = m.TeamB.TeamID,
                        TeamNumberInLevel = m.TeamB.TeamNumberInLevel,
                        school_name = m.TeamB.schools.school_name
                    },
                    Winner = m.Winner == null ? null : new MatchTeamDto
                    {
                        TeamID = m.Winner.TeamID,
                        TeamNumberInLevel = m.Winner.TeamNumberInLevel,
                        school_name = m.Winner.schools.school_name
                    }
                })
                .ToListAsync();
        }

        /// <summary>
        /// ดึงประวัติการสุ่ม (Draw Logs) ของ competition level
        /// </summary>
        public async Task<List<MatchDrawLogDto>> GetDrawLogsAsync(int competitionLevelID)
        {
            await EnsureTableCreatedAsync();
            return await _context.MatchDrawLogs
                .Where(l => l.CompetitionLevelID == competitionLevelID)
                .OrderByDescending(l => l.CreatedAt)
                .AsNoTracking()
                .Select(l => new MatchDrawLogDto
                {
                    Id = l.Id,
                    CompetitionLevelID = l.CompetitionLevelID,
                    Round = l.Round,
                    ActionType = l.ActionType,
                    Note = l.Note,
                    MatchesJson = l.MatchesJson,
                    CreatedAt = l.CreatedAt
                })
                .ToListAsync();
        }

        /// <summary>
        /// บันทึกผลผู้ชนะ แล้วคืน MatchDto
        /// </summary>
        public async Task<MatchDto> SubmitResultAsync(int matchId, int winnerTeamId, bool isEdit = false)
        {
            var match = await _context.Matches
                .Include(m => m.TeamA).ThenInclude(t => t.schools)
                .Include(m => m.TeamB).ThenInclude(t => t.schools)
                .Include(m => m.Winner).ThenInclude(t => t.schools)
                .FirstOrDefaultAsync(m => m.Id == matchId)
                ?? throw new KeyNotFoundException("Match not found.");

            var winner = await _context.Teams.FindAsync(winnerTeamId)
                ?? throw new KeyNotFoundException("Winning team not found.");

            bool isTeamA = match.TeamA?.TeamID == winner.TeamID;
            bool isTeamB = match.TeamB?.TeamID == winner.TeamID;

            if (match.TeamB != null && !isTeamA && !isTeamB)
                throw new InvalidOperationException("ผู้ชนะจะต้องเป็นหนึ่งในผู้เข้าร่วมการแข่งขัน");

            if (!isEdit && match.Status == "Completed")
                throw new InvalidOperationException("การแข่งขันเสร็จสิ้นแล้ว");

            match.Winner = winner;
            match.Status = "Completed";

            if (match.TeamA != null)
                match.TeamA.IsLuckyDrawWinner = match.TeamA.TeamID == winner.TeamID;

            if (match.TeamB != null)
                match.TeamB.IsLuckyDrawWinner = match.TeamB.TeamID == winner.TeamID;

            await _context.SaveChangesAsync();

            await _hubContext.Clients.Group(match.CompetitionLevelID.ToString())
               .SendAsync("ReceiveMatchUpdate", $"Match {match.Id} updated.");

            return _mapper.Map<MatchDto>(match);
        }

        /// <summary>
        /// สร้างสายการแข่งขันรอบแรกและคืน MatchDto ทั้งหมด
        /// </summary>
        public async Task<List<MatchDto>> GenerateMatchBracketAsync(int competitionLevelID, int? sampleSize = null)
        {
            var competitionLevel = await _context.CompetitionLevels
                .Include(t => t.Teams)
                .Include(t => t.Matches)
                .FirstOrDefaultAsync(t => t.CompetitionLevelID == competitionLevelID);

            if (competitionLevel == null)
                throw new KeyNotFoundException("ไม่พบการแข่งขัน.");

            _context.Matches.RemoveRange(competitionLevel.Matches);

            var approvedTeams = competitionLevel.Teams
                .Where(tt => tt.Status == SD.TeamStatus.CheckedIn)
                .ToList();

            if (approvedTeams.Count < 2)
                throw new InvalidOperationException("ต้องมีอย่างน้อย 2 ทีมที่ได้รับการอนุมัติเพื่อสร้างการแข่งขัน.");

            if (sampleSize.HasValue)
            {
                if (sampleSize.Value < 2)
                    throw new InvalidOperationException("sampleSize ต้องมากกว่าหรือเท่ากับ 2");
                if (sampleSize.Value > approvedTeams.Count)
                    throw new InvalidOperationException($"sampleSize มากกว่าจำนวนทีมที่มีอยู่ ({approvedTeams.Count})");

                approvedTeams = approvedTeams
                    .OrderBy(_ => Guid.NewGuid())
                    .Take(sampleSize.Value)
                    .ToList();
            }

            // สุ่มจับคู่เฉพาะรอบที่ 1
            var matches = GenerateBracket(approvedTeams, competitionLevelID);

            _context.Matches.AddRange(matches);
            await _context.SaveChangesAsync();

            await SaveDrawLogInternalAsync(competitionLevelID, 1, "GENERATE_FIRST_ROUND", $"สุ่มจับคู่รอบแรก จำนวน {matches.Count} คู่");

            await _hubContext.Clients.Group(competitionLevelID.ToString())
               .SendAsync("ReceiveMatchUpdate", "รอบแรกถูกสร้างแล้ว");

            return _mapper.Map<List<MatchDto>>(matches);
        }

        /// <summary>
        /// หารอบล่าสุดแล้วสร้างรอบถัดไปอัตโนมัติ
        /// </summary>
        public async Task<string> GenerateNextRoundAutoAsync(int competitionLevelID)
        {
            var lastRound = await _context.Matches
                .Where(m => m.CompetitionLevelID == competitionLevelID)
                .MaxAsync(m => (int?)m.Round) ?? 0;

            return await GenerateNextRoundAsync(competitionLevelID, lastRound);
        }

        /// <summary>
        /// สร้างรอบถัดไปจาก currentRound
        /// </summary>
        public async Task<string> GenerateNextRoundAsync(int competitionLevelID, int currentRound)
        {
            bool anyIncomplete = await _context.Matches
                .AnyAsync(m => m.CompetitionLevelID == competitionLevelID &&
                               m.Round == currentRound &&
                               m.Status != "Completed" &&
                               m.Status != "Bye");

            if (anyIncomplete)
                return $"ไม่สามารถสร้างรอบที่ {currentRound + 1} ได้ เพราะยังมีแมตช์ในรอบที่ {currentRound} ที่ยังไม่จบ";

            var winners = await _context.Matches
                .Where(m => m.CompetitionLevelID == competitionLevelID &&
                            m.Round == currentRound &&
                            (m.Status == "Completed" || m.Status == "Bye"))
                .Select(m => m.Winner)
                .Where(w => w != null)
                .ToListAsync();

            if (winners.Count == 1)
                return $"การแข่งขันจบแล้ว! ผู้ชนะคือ {winners[0].schools.school_name}";

            if (winners.Count < 2)
                return $"ไม่สามารถสร้างรอบที่ {currentRound + 1} ได้ เพราะมีผู้ชนะไม่เพียงพอ";

            var nextRound = currentRound + 1;
            var nextMatches = new List<Match>();

            var previousMatchups = await _context.Matches
                .Where(m => m.CompetitionLevelID == competitionLevelID && m.TeamA != null && m.TeamB != null)
                .Select(m => new { A = m.TeamA.TeamID, B = m.TeamB.TeamID })
                .ToListAsync();

            var shuffled = winners.OrderBy(_ => Guid.NewGuid()).ToList();
            var unpaired = new HashSet<Team>(shuffled);

            while (unpaired.Count > 1)
            {
                var teamA = unpaired.First();
                unpaired.Remove(teamA);

                var opponent = unpaired.FirstOrDefault(t =>
                    !previousMatchups.Any(m =>
                        (m.A == teamA.TeamID && m.B == t.TeamID) ||
                        (m.A == t.TeamID && m.B == teamA.TeamID)));

                if (opponent != null)
                {
                    unpaired.Remove(opponent);

                    nextMatches.Add(new Match
                    {
                        CompetitionLevelID = competitionLevelID,
                        Round = nextRound,
                        TeamA = teamA,
                        TeamB = opponent,
                        Status = "Pending",
                        MatchTime = DateTime.UtcNow.AddHours(7).AddDays(nextRound)
                    });
                }
                else
                {
                    var fallback = unpaired.First();
                    unpaired.Remove(fallback);

                    nextMatches.Add(new Match
                    {
                        CompetitionLevelID = competitionLevelID,
                        Round = nextRound,
                        TeamA = teamA,
                        TeamB = fallback,
                        Status = "Pending",
                        MatchTime = DateTime.UtcNow.AddHours(7).AddDays(nextRound)
                    });
                }
            }

            if (unpaired.Count == 1)
            {
                var solo = unpaired.First();
                nextMatches.Add(new Match
                {
                    CompetitionLevelID = competitionLevelID,
                    Round = nextRound,
                    TeamA = solo,
                    TeamB = null,
                    Winner = solo,
                    Status = "Bye",
                    MatchTime = DateTime.UtcNow.AddHours(7).AddDays(nextRound)
                });
            }

            await _context.Matches.AddRangeAsync(nextMatches);
            await _context.SaveChangesAsync();

            await SaveDrawLogInternalAsync(competitionLevelID, nextRound, "NEXT_ROUND", $"สุ่มสร้างรอบที่ {nextRound} จำนวน {nextMatches.Count} คู่");

            await _hubContext.Clients.Group(competitionLevelID.ToString())
              .SendAsync("ReceiveMatchUpdate", $"สร้างรอบที่ {nextRound} สำเร็จ");

            return $"สร้างรอบที่ {nextRound} สำเร็จ จำนวน {nextMatches.Count} คู่แข่งขัน";
        }

        /// <summary>
        /// ย้อนกลับไปใช้ผลการสุ่มจาก Log ในอดีต (Rollback State)
        /// </summary>
        public async Task<string> RollbackToLogAsync(int logId)
        {
            await EnsureTableCreatedAsync();
            var log = await _context.MatchDrawLogs.FindAsync(logId)
                ?? throw new KeyNotFoundException("ไม่พบประวัติการสุ่มที่ระบุ");

            if (string.IsNullOrEmpty(log.MatchesJson))
                throw new InvalidOperationException("ประวัติการสุ่มนี้ไม่มีข้อมูลสำหรับกู้คืน");

            var snapshotMatches = JsonSerializer.Deserialize<List<MatchSnapshotDto>>(log.MatchesJson);
            if (snapshotMatches == null || !snapshotMatches.Any())
                throw new InvalidOperationException("ไม่สามารถอ่านข้อมูลแมตช์ในประวัตินี้ได้");

            var currentMatches = await _context.Matches
                .Where(m => m.CompetitionLevelID == log.CompetitionLevelID)
                .ToListAsync();
            _context.Matches.RemoveRange(currentMatches);

            var restoredMatches = new List<Match>();
            foreach (var snap in snapshotMatches)
            {
                Team? teamA = snap.TeamAId.HasValue ? await _context.Teams.FindAsync(snap.TeamAId.Value) : null;
                Team? teamB = snap.TeamBId.HasValue ? await _context.Teams.FindAsync(snap.TeamBId.Value) : null;
                Team? winner = snap.WinnerId.HasValue ? await _context.Teams.FindAsync(snap.WinnerId.Value) : null;

                restoredMatches.Add(new Match
                {
                    CompetitionLevelID = log.CompetitionLevelID,
                    Round = snap.Round,
                    TeamA = teamA,
                    TeamB = teamB,
                    Winner = winner,
                    Status = snap.Status ?? "Pending",
                    MatchTime = DateTime.UtcNow.AddHours(7).AddDays(snap.Round)
                });
            }

            await _context.Matches.AddRangeAsync(restoredMatches);
            await _context.SaveChangesAsync();

            await SaveDrawLogInternalAsync(log.CompetitionLevelID, log.Round, "ROLLBACK", $"ย้อนกลับไปใช้ผลการสุ่ม Log #{log.Id} (รอบที่ {log.Round})");

            await _hubContext.Clients.Group(log.CompetitionLevelID.ToString())
               .SendAsync("ReceiveMatchUpdate", $"ย้อนกลับไปใช้ผลการสุ่มรอบที่ {log.Round} สำเร็จ");

            return $"ย้อนกลับไปใช้ผลการสุ่ม Log #{log.Id} สำเร็จ (รอบที่ {log.Round})";
        }

        /// <summary>
        /// ยกเลิกรอบตั้งแต่ roundNumber ขึ้นไป
        /// </summary>
        public async Task<string> CancelRoundAsync(int competitionLevelID, int roundNumber)
        {
            var matchesToCancel = await _context.Matches
                .Where(m => m.CompetitionLevelID == competitionLevelID && m.Round >= roundNumber)
                .ToListAsync();

            if (!matchesToCancel.Any())
                return $"ไม่พบแมตช์ในรอบที่ {roundNumber} ขึ้นไปให้ยกเลิก";

            var affectedRounds = matchesToCancel
                .Select(m => m.Round)
                .Distinct()
                .OrderBy(r => r)
                .ToList();

            _context.Matches.RemoveRange(matchesToCancel);

            if (roundNumber > 1)
            {
                var previousRoundMatches = await _context.Matches
                    .Where(m => m.CompetitionLevelID == competitionLevelID && m.Round == roundNumber - 1)
                    .ToListAsync();

                foreach (var match in previousRoundMatches)
                {
                    match.Winner = null;
                }
            }

            await _context.SaveChangesAsync();

            await SaveDrawLogInternalAsync(competitionLevelID, roundNumber, "CANCEL_ROUND", $"ยกเลิกรอบที่ {string.Join(", ", affectedRounds)}");

            await _hubContext.Clients.Group(competitionLevelID.ToString())
               .SendAsync("ReceiveMatchUpdate", $"ยกเลิกรอบสำเร็จ");

            return $"ยกเลิกรอบที่ {string.Join(", ", affectedRounds)} สำเร็จ รวม {matchesToCancel.Count} แมตช์ถูกลบ";
        }

        /// <summary>
        /// ภายใน: สร้าง Snapshot และลงบันทึกใน MatchDrawLog
        /// </summary>
        private async Task SaveDrawLogInternalAsync(int competitionLevelID, int round, string actionType, string note)
        {
            await EnsureTableCreatedAsync();
            var matches = await GetMatchesByCompetitionLevelIdAsync(competitionLevelID);
            var snapshotList = matches.Select(m => new MatchSnapshotDto
            {
                Id = m.Id,
                Round = m.Round,
                TeamAId = m.TeamA?.TeamID,
                TeamBId = m.TeamB?.TeamID,
                WinnerId = m.Winner?.TeamID,
                Status = m.Status
            }).ToList();

            var json = JsonSerializer.Serialize(snapshotList);

            var drawLog = new Models.MatchDrawLog
            {
                CompetitionLevelID = competitionLevelID,
                Round = round,
                ActionType = actionType,
                Note = note,
                MatchesJson = json,
                CreatedAt = DateTime.UtcNow.AddHours(7)
            };

            _context.MatchDrawLogs.Add(drawLog);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// สุ่มจับคู่เฉพาะรอบที่ 1 จากรายการทีมทั้งหมด
        /// </summary>
        private List<Match> GenerateBracket(List<Team> teams, int competitionLevelID)
        {
            var matches = new List<Match>();
            var currentTeams = teams.OrderBy(_ => Guid.NewGuid()).ToList();
            var round = 1;

            for (int i = 0; i < currentTeams.Count; i += 2)
            {
                if (i + 1 < currentTeams.Count)
                {
                    matches.Add(new Match
                    {
                        CompetitionLevelID = competitionLevelID,
                        Round = round,
                        TeamA = currentTeams[i],
                        TeamB = currentTeams[i + 1],
                        Status = "Pending",
                        MatchTime = DateTime.UtcNow.AddHours(7).AddDays(round)
                    });
                }
                else
                {
                    matches.Add(new Match
                    {
                        CompetitionLevelID = competitionLevelID,
                        Round = round,
                        TeamA = currentTeams[i],
                        TeamB = null,
                        Winner = currentTeams[i],
                        Status = "Bye",
                        MatchTime = DateTime.UtcNow.AddHours(7).AddDays(round)
                    });
                }
            }

            return matches;
        }
    }

    public class MatchSnapshotDto
    {
        public int Id { get; set; }
        public int Round { get; set; }
        public int? TeamAId { get; set; }
        public int? TeamBId { get; set; }
        public int? WinnerId { get; set; }
        public string? Status { get; set; }
    }
}