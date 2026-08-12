using System;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using ProjectScientDay2568.Data;
using ProjectScientDay2568.Models;
using ProjectScientDay2568.Models.Dtos.Team;
using ProjectScientDay2568.Utility;
using static ProjectScientDay2568.Utility.SD;
namespace ProjectScientDay2568.Service
{
    public class TeamService : ITeamService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly UserContextService _userContext;

        public TeamService(ApplicationDbContext context, IMapper mapper, UserContextService userContext)
        {
            _context = context;
            _mapper = mapper;
            _userContext = userContext;
        }

        private async Task EnsureRegistrationOpenAsync(int? competitionLevelId)
        {
            if (_userContext.IsStaffOrAdmin) return;

            if (!competitionLevelId.HasValue)
                throw new KeyNotFoundException("ไม่พบระดับการแข่งขัน");

            var closeDate = await _context.CompetitionLevels
                .Where(level => level.CompetitionLevelID == competitionLevelId.Value)
                .Select(level => level.CompetitionYear!.Year!.RegistrationCloseDate)
                .SingleOrDefaultAsync();

            if (closeDate.HasValue && DateTime.Now > closeDate.Value)
                throw new InvalidOperationException("ปิดรับลงทะเบียนแล้ว ไม่สามารถแก้ไขหรือจัดการทีมได้");
        }
        public async Task<List<TeamDto>> GetAllTeamAsync()
        {
            return await _context.Teams
                 .Include(t => t.CompetitionLevel)
                 .ThenInclude(cl => cl.CompetitionYear)
                 .ThenInclude(cy => cy.Competition)
                 .Include(t => t.CompetitionLevel.EducationLevel)
                 .Include(t => t.RewardType)
                 .Include(t => t.schools)
                 .Include(t => t.Students)
                 .ProjectTo<TeamDto>(_mapper.ConfigurationProvider)
                 .ToListAsync();
        }
        public async Task<List<TeamDto>> GetAllTeamUserAsync(string UserId)
        {

            if (string.IsNullOrEmpty(UserId))
            {
                throw new ArgumentNullException(nameof(UserId), "UserId cannot be null or empty.");
            }
            var team = await _context.Teams
                .Where(t => t.UserId == UserId)
                .Include(t => t.CompetitionLevel)
                .ThenInclude(cl => cl.CompetitionYear)
                .ThenInclude(cy => cy.Competition)
                .Include(t => t.CompetitionLevel.EducationLevel)
                .Include(t => t.schools)
                .OrderBy(t => t.Status)
                .ToListAsync();
            return _mapper.Map<List<TeamDto>>(team);
        }
        public async Task<TeamStatusGroupDto> GetAllTeamStatusAsync(int competitionLevelID)
        {
            var baseQuery = _context.Teams
                .Where(t =>
                    (t.Status == SD.TeamStatus.CheckedIn ||
                     t.Status == SD.TeamStatus.Verified ||
                     t.Status == SD.TeamStatus.ProjectRevision ||
                     t.Status == SD.TeamStatus.Disqualified)
                    && t.CompetitionLevelID == competitionLevelID);

            // ✅ ให้ EF ดึงเฉพาะฟิลด์ที่ TeamDto ต้องใช้ (ผ่าน AutoMapper)
            var all = await baseQuery
                .ProjectTo<TeamDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            // แยกกลุ่มตามที่ต้องการ
            var checkedInTeams = all
                .Where(t => t.Status == SD.TeamStatus.CheckedIn)
                .OrderBy(t => t.RewardTypeID)
                .ToList();

            var verifiedTeams = all
                .Where(t => t.Status == SD.TeamStatus.Verified
                         || t.Status == SD.TeamStatus.ProjectRevision
                         || t.Status == SD.TeamStatus.CheckedIn
                         || t.Status == SD.TeamStatus.Disqualified)
                .ToList();

            return new TeamStatusGroupDto
            {
                CheckedInTeams = checkedInTeams,
                VerifiedTeams = verifiedTeams
            };
        }
        public async Task<TeamCopyResultDto> CopyTeamAsync(TeamCopyRequest req, string userId)
        {
            if (req == null || req.SourceTeamId <= 0) throw new ArgumentException("ข้อมูลไม่ถูกต้อง");

            // โหลดทีมต้นฉบับ + Students (no-tracking)
            var src = await _context.Teams
                .AsNoTracking()
                .Include(t => t.Students)
                .FirstOrDefaultAsync(t => t.TeamID == req.SourceTeamId);

            if (src == null) throw new Exception("ไม่พบทีมต้นทาง");

            var targetLevelId = req.TargetCompetitionLevelID ?? src.CompetitionLevelID;
            if (targetLevelId == null) throw new Exception("ไม่ทราบระดับการแข่งขันปลายทาง");
            await EnsureRegistrationOpenAsync(targetLevelId);

            using var tx = await _context.Database.BeginTransactionAsync();

            // หา TeamNumberInLevel ใหม่ (ต่อท้าย)
            var nextTeamNumberInLevel = (await _context.Teams
                .Where(t => t.CompetitionLevelID == targetLevelId)
                .MaxAsync(t => (int?)t.TeamNumberInLevel)) ?? 0;
            nextTeamNumberInLevel += 1;

            // หา NameOrdinal ใหม่ ภายใน level เดียวกัน + (ถ้ามี) school_code เดียวกัน
            int nextOrdinal;
            if (src.school_code.HasValue)
            {
                var maxOrdinal = await _context.Teams
                    .Where(t => t.CompetitionLevelID == targetLevelId && t.school_code == src.school_code)
                    .MaxAsync(t => (int?)t.NameOrdinal) ?? -1;
                nextOrdinal = maxOrdinal + 1;
            }
            else
            {
                // ถ้าไม่มี school_code ให้รัน ordinal รวมใน level
                var maxOrdinal = await _context.Teams
                    .Where(t => t.CompetitionLevelID == targetLevelId)
                    .MaxAsync(t => (int?)t.NameOrdinal) ?? -1;
                nextOrdinal = maxOrdinal + 1;
            }

            var now = DateTime.Now;

            var newTeam = new Team
            {
                CompetitionLevelID = targetLevelId,
                TeamNumberInLevel = nextTeamNumberInLevel,
                NameOrdinal = nextOrdinal,

                // โรงเรียน (คงค่า school_code เดิม; nav 'schools' ให้ EF เติมภายหลังถ้า tracked)
                school_code = src.school_code,

                // ผู้จัดการทีม
                PrimaryTitlePrefix = src.PrimaryTitlePrefix,
                PrimaryFirstName = src.PrimaryFirstName,
                PrimaryLastName = src.PrimaryLastName,
                SecondaryTitlePrefix = src.SecondaryTitlePrefix,
                SecondaryFirstName = src.SecondaryFirstName,
                SecondaryLastName = src.SecondaryLastName,

                // ผู้ใช้
                UserId = userId,

                // สถานะ
                Status = TeamStatus.Verified,
                StatusTime = now,
                RegistrationDateTime = now,

                // ข้อมูลงาน
                ProjectName = string.IsNullOrWhiteSpace(req.OverrideProjectName) ? src.ProjectName : req.OverrideProjectName,
                ProjectPDFLink = null,     // ไม่ carry ไฟล์เดิม

                // รางวัล
                RewardTypeID = 5,       // ตัดรางวัลเดิมทิ้ง
                IsLuckyDrawWinner = false, // รีเซ็ต
            };

            // คัดลอก Students (ถ้าต้องการ)
            if (req.IncludeStudents && (src.Students?.Any() ?? false))
            {
                newTeam.Students = src.Students!.Select(s => new Student
                {
                    TitlePrefix = s.TitlePrefix,
                    FirstName = s.FirstName,
                    LastName = s.LastName,

                }).ToList();
            }

            _context.Teams.Add(newTeam);

            try
            {
                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch (DbUpdateException ex)
            {
                await tx.RollbackAsync();
                // ถ้ามี Unique Index ซ้ำ ให้สื่อสารชัดเจน
                throw new Exception("คัดลอกทีมล้มเหลว: พบเลขทีม/ลำดับซ้ำในระดับนี้ กรุณาลองใหม่", ex);
            }

            return new TeamCopyResultDto
            {
                NewTeamId = newTeam.TeamID,
                CompetitionLevelID = newTeam.CompetitionLevelID,
                TeamNumberInLevel = newTeam.TeamNumberInLevel,
                NameOrdinal = newTeam.NameOrdinal
            };
        }
        public async Task<List<TeamDto>> GetAllTeamcompetitionLevelAsync(int competitionLevelID, string userId)
        {
            var team = await _context.Teams
                .Where(p => p.UserId == userId &&
                    (p.CompetitionLevelID == competitionLevelID || p.CompetitionLevelID == null))
                .Include(p => p.CompetitionLevel)
                  .ThenInclude(p => p.CompetitionYear)
                .Include(p => p.CompetitionLevel)
                  .ThenInclude(p => p.CompetitionYear)
                    .ThenInclude(p => p.Competition)
                .Include(p => p.CompetitionLevel)
                  .ThenInclude(p => p.EducationLevel)
                  .Include(p => p.RewardType)
                .Include(p => p.Students)
                 .Include(t => t.schools)
                .Include(t => t.TeamProjects)
                .ToListAsync();
            var teamDtos = _mapper.Map<List<TeamDto>>(team);

            var teamIds = team.Select(t => t.TeamID).ToList();
            var certTeamIds = await _context.TeamCertificates
                .Where(tc => teamIds.Contains(tc.TeamID))
                .Select(tc => tc.TeamID)
                .Distinct()
                .ToListAsync();

            foreach (var teamDto in teamDtos)
            {
                teamDto.HasCertificate = certTeamIds.Contains(teamDto.TeamID);

                var sourceTeam = team.FirstOrDefault(t => t.TeamID == teamDto.TeamID);
                var latestProject = sourceTeam?.TeamProjects
                    .OrderByDescending(project => project.TeamProjectID)
                    .FirstOrDefault();

                if (latestProject != null)
                {
                    teamDto.ProjectName = latestProject.ProjectName;
                    teamDto.ProjectPDFLink = latestProject.ProjectPDFLink;
                }
            }

            return teamDtos;
        }
        public async Task<TeamCreateDto> CreateTeamAsync(
     TeamCreateDto teamDto,
     string userId,
     IFormFile? file = null,
     string? projectName = null)
        {
            if (teamDto == null)
                throw new ArgumentNullException(nameof(teamDto), "team cannot be null.");

            await EnsureRegistrationOpenAsync(teamDto.CompetitionLevelID);

            var hasSchool = await _context.School
                .AnyAsync(s => s.school_code == teamDto.school_code);

            if (!hasSchool)
                throw new ArgumentException($"ไม่พบโรงเรียน code={teamDto.school_code}");

            var maxOrdinal = await _context.Teams
                .Where(t => t.CompetitionLevelID == teamDto.CompetitionLevelID
                    && t.school_code == teamDto.school_code)
                .MaxAsync(t => (int?)t.NameOrdinal) ?? -1;

            int nextOrdinal = maxOrdinal + 1;

            int nextTeamNumberInLevel = (await _context.Teams
                .Where(t => t.CompetitionLevelID == teamDto.CompetitionLevelID)
                .MaxAsync(t => (int?)t.TeamNumberInLevel) ?? 0) + 1;

            var team = _mapper.Map<Team>(teamDto);
            team.NameOrdinal = nextOrdinal;
            team.TeamNumberInLevel = nextTeamNumberInLevel;
            team.UserId = userId;
            team.RegistrationDateTime = DateTime.Now;
            team.RewardTypeID = 5;
            team.Status = SD.TeamStatus.Verified;

            if (file != null && file.Length > 0)
            {
                var uploadsFolder = Path.Combine("wwwroot", "uploads", "projects");

                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                team.ProjectPDFLink = $"/uploads/projects/{uniqueFileName}";
                team.ProjectName = projectName ?? string.Empty;
            }

            await _context.Teams.AddAsync(team);
            await _context.SaveChangesAsync();

            return _mapper.Map<TeamCreateDto>(team);
        }

        public async Task<TeamCreateDto> UpdateTeamAsync(int id, TeamCreateDto teamDto, string userId)
        {
            if (teamDto == null)
                throw new ArgumentNullException(nameof(teamDto), "team cannot be null.");

            var teamToUpdate = await _context.Teams.FindAsync(id);
            if (teamToUpdate == null)
                throw new KeyNotFoundException($"team with ID {id} not found.");

            if (teamToUpdate.UserId != userId && teamToUpdate.UserId == "Admin")
                throw new UnauthorizedAccessException("คุณไม่มีสิทธิ์แก้ไขทีมนี้");

            await EnsureRegistrationOpenAsync(teamToUpdate.CompetitionLevelID);

            // 🛡️ กัน TeamNumberInLevel ถูกเขียนทับ
            var keepTeamNumberInLevel = teamToUpdate.TeamNumberInLevel;

            _mapper.Map(teamDto, teamToUpdate);

            teamToUpdate.TeamNumberInLevel = keepTeamNumberInLevel; // ตั้งกลับเป็นค่าดั้งเดิม

            await _context.SaveChangesAsync();
            return _mapper.Map<TeamCreateDto>(teamToUpdate);
        }
        public async Task<TeamDto> ChangStatusteam(int teamID, TeamStatus status)
        {
            var team = await _context.Teams.FindAsync(teamID);

            if (team == null)
                throw new KeyNotFoundException($"Team with ID {teamID} not found.");

            team.Status = status;
            team.StatusTime = DateTime.Now;


            await _context.SaveChangesAsync();

            return _mapper.Map<TeamDto>(team);
        }
        public async Task<TeamDto> TeamAward(int teamID, int rewardTypeID)
        {
            var team = await _context.Teams.FindAsync(teamID);

            if (team == null)
                throw new KeyNotFoundException($"Team with ID {teamID} not found.");

            // ตรวจสอบว่า Award มีอยู่และตรงกับระดับการแข่งขัน
            var award = await _context.RewardTypes
                .FirstOrDefaultAsync(a => a.RewardTypeID == rewardTypeID);

            // กำหนดรางวัลให้ทีม
            team.RewardTypeID = rewardTypeID;

            await _context.SaveChangesAsync();

            return _mapper.Map<TeamDto>(team);
        }
        public async Task DeleteTeamAsync(int id)
        {
            var team = await _context.Teams.FindAsync(id);

            if (team == null) throw new KeyNotFoundException("ไม่พบทีม");

            await EnsureRegistrationOpenAsync(team.CompetitionLevelID);

            _context.Teams.Remove(team);
            await _context.SaveChangesAsync();
        }
        public async Task<TeamRegisterDto> UpdateTeamRegistrationAsync(
        int teamId, string userId, TeamRegisterDto teamRegister, int competitionLevelId)
        {
            var team = await _context.Teams.FindAsync(teamId);
            if (team == null)
                throw new KeyNotFoundException($"Team with ID {teamId} not found.");

            var competitionLevel = await _context.CompetitionLevels
                .FirstOrDefaultAsync(c => c.CompetitionLevelID == competitionLevelId);
            if (competitionLevel == null)
                throw new KeyNotFoundException($"CompetitionLevel with ID {competitionLevelId} not found.");

            await EnsureRegistrationOpenAsync(competitionLevelId);

            // ✅ ตรวจสอบจำนวนสมาชิกในทีม
            var studentCount = await _context.Students
                .Where(s => s.TeamID == teamId)
                .CountAsync();

            //if (competitionLevel.MinTeamMembers.HasValue && studentCount < competitionLevel.MinTeamMembers.Value)
            //    throw new InvalidOperationException($"ทีมต้องมีสมาชิกอย่างน้อย {competitionLevel.MinTeamMembers} คน");

            //if (competitionLevel.MaxTeamMembers.HasValue && studentCount > competitionLevel.MaxTeamMembers.Value)
            //    throw new InvalidOperationException($"ทีมต้องมีสมาชิกไม่เกิน {competitionLevel.MaxTeamMembers} คน");
             
            team.Status = SD.TeamStatus.Verified;
            team.StatusTime = DateTime.Now;
            team.CompetitionLevelID = competitionLevelId;
            team.UserId = userId;

            await _context.SaveChangesAsync();
            return _mapper.Map<TeamRegisterDto>(team);
        }
    }
}
