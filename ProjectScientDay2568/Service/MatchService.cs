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
        /// ดึงแมตช์ทั้งหมดของ competition level เป็น DTO
        /// ใช้ ProjectTo เพื่อให้ EF สร้าง DTO จาก DB โดยตรง (ไม่ต้อง Include)
        /// </summary>
        public async Task<List<MatchDto>> GetMatchesByCompetitionLevelIdAsync(int competitionLevelID)
        {
            return await _context.Matches
             .Where(m => m.CompetitionLevelID == competitionLevelID)
             .Include(m => m.TeamA).ThenInclude(t => t.schools)
             .Include(m => m.TeamB).ThenInclude(t => t.schools)
             .Include(m => m.Winner).ThenInclude(t => t.schools)
             .AsNoTracking()
             .ProjectTo<MatchDto>(_mapper.ConfigurationProvider)
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

            // ✅ ตรวจว่าผู้ชนะต้องเป็นหนึ่งในผู้เข้าแข่งขัน เว้นกรณี TeamB == null (bye)
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

            // ✅ map entity ที่โหลด navigation มาแล้ว เป็น DTO
            return _mapper.Map<MatchDto>(match);
        }

        /// <summary>
        /// สร้างสายการแข่งขันรอบแรกและคืน MatchDto ทั้งหมด
        /// </summary>
        // เปลี่ยนเมธอดเดิมให้รับ sampleSize (เลือกสุ่มกี่ทีม) ได้
        public async Task<List<MatchDto>> GenerateMatchBracketAsync(int competitionLevelID, int? sampleSize = null)
        {
            var competitionLevel = await _context.CompetitionLevels
                .Include(t => t.Teams)
                .Include(t => t.Matches)
                .FirstOrDefaultAsync(t => t.CompetitionLevelID == competitionLevelID);

            if (competitionLevel == null)
                throw new KeyNotFoundException("ไม่พบการแข่งขัน.");

            // ลบแมตช์เดิมทั้งหมดของ level นี้
            _context.Matches.RemoveRange(competitionLevel.Matches);

            // ทีมที่ได้รับการอนุมัติ/เช็คอิน
            var approvedTeams = competitionLevel.Teams
                .Where(tt => tt.Status == SD.TeamStatus.CheckedIn)
                .ToList();

            if (approvedTeams.Count < 2)
                throw new InvalidOperationException("ต้องมีอย่างน้อย 2 ทีมที่ได้รับการอนุมัติเพื่อสร้างการแข่งขัน.");

            // ถ้ามีการระบุจำนวนทีมที่จะสุ่ม (สำหรับเทส)
            if (sampleSize.HasValue)
            {
                if (sampleSize.Value < 2)
                    throw new InvalidOperationException("sampleSize ต้องมากกว่าหรือเท่ากับ 2");
                if (sampleSize.Value > approvedTeams.Count)
                    throw new InvalidOperationException($"sampleSize มากกว่าจำนวนทีมที่มีอยู่ ({approvedTeams.Count})");

                // สุ่มเลือกทีมตามจำนวนที่กำหนด
                approvedTeams = approvedTeams
                    .OrderBy(_ => Guid.NewGuid())
                    .Take(sampleSize.Value)
                    .ToList();
            }

            var matches = GenerateBracket(approvedTeams, competitionLevelID);

            _context.Matches.AddRange(matches);
            await _context.SaveChangesAsync();

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
        /// สร้างรอบถัดไปจาก currentRound พร้อมเลี่ยงการจับคู่ซ้ำถ้าเป็นไปได้
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

            // ดึงประวัติการจับคู่ทั้งหมดใน competition นี้
            var previousMatchups = await _context.Matches
                .Where(m => m.CompetitionLevelID == competitionLevelID && m.TeamA != null && m.TeamB != null)
                .Select(m => new { A = m.TeamA.TeamID, B = m.TeamB.TeamID })
                .ToListAsync();

            // สุ่มทีมและพยายามหลีกเลี่ยงทีมที่เคยเจอกัน
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
                    // หากเลี่ยงไม่ได้ ให้จับคู่ทีมแรกที่เหลือ
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

            // ถ้าเหลือทีมเดียว (bye)
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

            await _hubContext.Clients.Group(competitionLevelID.ToString())
              .SendAsync("ReceiveMatchUpdate", $"สร้างรอบที่ {nextRound} สำเร็จ");

            return $"สร้างรอบที่ {nextRound} สำเร็จ จำนวน {nextMatches.Count} คู่แข่งขัน";
        }

        /// <summary>
        /// ยกเลิกรอบตั้งแต่ roundNumber ขึ้นไป และ reset ผู้ชนะในรอบก่อนหน้า
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

            // คืนค่าผู้ชนะในรอบก่อนหน้า (หากมี)
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

            await _hubContext.Clients.Group(competitionLevelID.ToString())
               .SendAsync("ReceiveMatchUpdate", $"ยกเลิกรอบสำเร็จ");

            return $"ยกเลิกรอบที่ {string.Join(", ", affectedRounds)} สำเร็จ รวม {matchesToCancel.Count} แมตช์ถูกลบ";
        }

        /// <summary>
        /// ใช้ภายใน: สร้างสายรอบแรก พร้อมรองรับ bye
        /// </summary>
        private List<Match> GenerateBracket(List<Team> teams, int competitionLevelID)
        {
            var matches = new List<Match>();
            var currentTeams = teams.OrderBy(_ => Guid.NewGuid()).ToList();
            var round = 1;

            while (currentTeams.Count > 1)
            {
                var nextRound = new List<Team>();

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

                        nextRound.Add(currentTeams[i]);
                    }
                }

                currentTeams = nextRound;
                round++;
            }

            return matches;
        }
    }
}
