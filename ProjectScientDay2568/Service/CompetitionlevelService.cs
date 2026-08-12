using Microsoft.EntityFrameworkCore;
using AutoMapper;
using ProjectScientDay2568.Data;
using ProjectScientDay2568.Models;
using ProjectScientDay2568.Models.Dtos.Team;
using static ProjectScientDay2568.Utility.SD;

namespace ProjectScientDay2568.Service
{
    public class CompetitionlevelService : ICompetitionlevelService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public CompetitionlevelService(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<List<CompetitionLevelDto>> GetCompetitionLevelListAsync(int competitionYearID)
        {
            var competitionLevels = await _context.CompetitionLevels
                                            .Where(p => p.CompetitionYearID == competitionYearID)
                                            .Include(p => p.EducationLevel)
                                            .Include(p => p.CompetitionYear).ThenInclude(y => y.Competition)
                                            .AsNoTracking().ToListAsync();
            var result = _mapper.Map<List<CompetitionLevelDto>>(competitionLevels);
            for (var i = 0; i < result.Count; i++)
            {
                result[i].AdvisorCount = competitionLevels[i].CompetitionYear?.AdvisorCount ?? 1;
                result[i].RegistrationMode = competitionLevels[i].CompetitionYear?.RegistrationMode ?? "team";
            }
            return result;
        }

        public async Task<CompetitionLevelDto> GetCompetitionLevelByIdAsync(int id)
        {
            var competitionLevels = await _context.CompetitionLevels
                .Include(x => x.CompetitionYear).ThenInclude(y => y.Competition)
                .FirstOrDefaultAsync(x => x.CompetitionLevelID == id);
            if (competitionLevels == null)
            {
                throw new KeyNotFoundException($"competitionLevels with ID {id} not found.");
            }
            var result = _mapper.Map<CompetitionLevelDto>(competitionLevels);
            result.AdvisorCount = competitionLevels.CompetitionYear?.AdvisorCount ?? 1;
            result.RegistrationMode = competitionLevels.CompetitionYear?.RegistrationMode ?? "team";
            return result;
        }

        public async Task<CompetitionLevelDto> CreateCompetitionLevel(CompetitionLevelDto competitionLevelDto)
        {
            if (competitionLevelDto == null)
            {
                throw new ArgumentNullException(nameof(competitionLevelDto), "competitionLevelDto cannot be null.");
            }

            var competitionLevel = _mapper.Map<CompetitionLevel>(competitionLevelDto);

            await _context.CompetitionLevels.AddAsync(competitionLevel);
            await _context.SaveChangesAsync();

            return _mapper.Map<CompetitionLevelDto>(competitionLevelDto);
        }

        public async Task<CompetitionLevelDto> UpdateCompetitionLevel(int id, CompetitionLevelDto competitionLevelDto)
        {
            if (competitionLevelDto == null)
            {
                throw new ArgumentNullException(nameof(competitionLevelDto), "competitionLevel cannot be null.");
            }

            var competitionLevelToUpdate = await _context.CompetitionLevels.FindAsync(id);
            if (competitionLevelToUpdate == null)
            {
                throw new KeyNotFoundException($"competitionLevel with ID {id} not found.");
            }

            _mapper.Map(competitionLevelDto, competitionLevelToUpdate);

            await _context.SaveChangesAsync();
            return _mapper.Map<CompetitionLevelDto>(competitionLevelToUpdate);
        }

        public async Task DeleteCompetitionLevel(int id)
        {
            var competitionLevel = await _context.CompetitionLevels.FindAsync(id);

            if (competitionLevel == null) throw new KeyNotFoundException("competitionLevel not found");

            _context.CompetitionLevels.Remove(competitionLevel);
            await _context.SaveChangesAsync();
        }

        //รันเลขเกียรติบัตรของทีมในระดับชั้นนั้น ๆ
        public async Task<List<TeamCertificateDto>> GenerateTeamCertificatesNumberByLevel(
            int competitionLevelId, bool forceRebuild = false, CancellationToken ct = default)
        {
            var level = await _context.CompetitionLevels
                .Include(cl => cl.CompetitionYear).ThenInclude(cy => cy.Year)
                .SingleOrDefaultAsync(cl => cl.CompetitionLevelID == competitionLevelId, ct)
                ?? throw new InvalidOperationException("ไม่พบระดับชั้น");

            var yearId = level.CompetitionYear.Year.YearID;
            var yearNumber = level.CompetitionYear.Year.YearNumber;

            var config = await _context.CertificateConfigs
                .AsTracking()
                .FirstOrDefaultAsync(c => c.YearID == yearId, ct)
                ?? throw new InvalidOperationException("ยังไม่ได้ตั้งค่าเกียรติบัตรสำหรับปีนี้");

            // ทีมที่ควรได้เลขในรอบนี้
            var teams = await _context.Teams
                .Where(t => t.Status == TeamStatus.CheckedIn && t.CompetitionLevelID == competitionLevelId)
                .OrderBy(t => t.RewardTypeID)
                .Select(t => new { t.TeamID })
                .ToListAsync(ct);

            // ถ้ายังไม่มีทีม → คืนว่าง และไม่แตะเลข
            if (teams.Count == 0)
                return new List<TeamCertificateDto>();

            var teamIds = teams.Select(x => x.TeamID).ToHashSet();

            // ใบเกียรติบัตรที่มีอยู่แล้วของ level นี้
            var existingInLevel = await _context.TeamCertificates
                .Where(tc => teamIds.Contains(tc.TeamID))
                .Select(tc => new { tc.TeamID, tc.CertificateNumber, tc.CertificateName, tc.IssuedDate })
                .ToListAsync(ct);

            // เช็คว่า “ออกครบชุดเดิมแล้วหรือยัง”
            bool sameSet = existingInLevel.Count == teamIds.Count &&
                           existingInLevel.All(x => teamIds.Contains(x.TeamID));

            // ถ้าออกครบชุดเดิมแล้ว และไม่ได้บังคับ rebuild → คืนค่าเดิม (idempotent)
            if (sameSet && !forceRebuild)
            {
                if (!level.IsCertificateNumberGenerated)
                {
                    level.IsCertificateNumberGenerated = true;
                    await _context.SaveChangesAsync(ct);
                }
                var certs = await _context.TeamCertificates
                    .Where(tc => teamIds.Contains(tc.TeamID))
                    .ToListAsync(ct);
                return _mapper.Map<List<TeamCertificateDto>>(certs);
            }

            // เริ่มทรานแซกชันความเข้มสูง
            await using var tx = await _context.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable, ct);

            // หาเลขสูงสุดทั้งปี + เทียบ config.StartNumber
            var existingNumbersInYear = await _context.TeamCertificates
                .Where(tc => tc.Team.CompetitionLevel.CompetitionYear.Year.YearID == yearId)
                .Select(tc => tc.CertificateNumber)
                .ToListAsync(ct);

            int maxExisting = 0;
            foreach (var no in existingNumbersInYear)
            {
                if (string.IsNullOrWhiteSpace(no)) continue;
                var slash = no.IndexOf('/');
                if (slash <= 0) continue;
                if (int.TryParse(no[..slash], out var n))
                    maxExisting = Math.Max(maxExisting, n);
            }

            // baseline เริ่มจาก max(config.StartNumber, maxExisting + 1)
            var runningNumber = Math.Max(config.StartNumber, maxExisting + 1);

            // ถ้ามีของเดิมใน level และ forceRebuild = true → พยายาม “คงเลขเดิม”
            int? minOldInLevel = null;
            if (existingInLevel.Count > 0 && forceRebuild && sameSet)
            {
                var nums = existingInLevel
                    .Select(x => {
                        var slash = x.CertificateNumber.IndexOf('/');
                        if (slash <= 0) return (int?)null;
                        if (int.TryParse(x.CertificateNumber[..slash], out var n)) return (int?)n;
                        return null;
                    })
                    .Where(n => n.HasValue).Select(n => n!.Value).ToList();

                if (nums.Count == existingInLevel.Count)
                    minOldInLevel = nums.Min();
            }

            if (minOldInLevel.HasValue)
                runningNumber = Math.Min(runningNumber, minOldInLevel.Value);

            // ลบของเดิมเฉพาะทีมใน level นี้ ถ้าจะสร้างใหม่จริง ๆ
            if (existingInLevel.Count > 0)
            {
                var toRemove = await _context.TeamCertificates
                    .Where(tc => teamIds.Contains(tc.TeamID))
                    .ToListAsync(ct);
                _context.TeamCertificates.RemoveRange(toRemove);
                await _context.SaveChangesAsync(ct);
            }

            // โหลดทีมพร้อม include เพื่อสร้าง entity
            var teamsWithNav = await _context.Teams
                .Where(t => teamIds.Contains(t.TeamID))
                .OrderBy(t => t.RewardTypeID)
                .Include(t => t.CompetitionLevel).ThenInclude(cl => cl.CompetitionYear).ThenInclude(cy => cy.Year)
                .ToListAsync(ct);

            var certName = (config.CertificateName ?? string.Empty).Trim();
            var newCerts = new List<TeamCertificate>(teamsWithNav.Count);
            foreach (var team in teamsWithNav)
            {
                newCerts.Add(new TeamCertificate
                {
                    TeamID = team.TeamID,
                    CertificateNumber = $"{runningNumber}/{yearNumber}",
                    CertificateName = certName,
                    IssuedDate = DateTime.UtcNow
                });

                runningNumber++;
            }

            if (newCerts.Count > 0)
                await _context.TeamCertificates.AddRangeAsync(newCerts, ct);

            // ไม่แก้ไขค่า config.StartNumber ในฐานข้อมูล เพื่อคงค่าเริ่มต้น (StartNumber) ที่ผู้ใช้ตั้งไว้ (เช่น 50)
            // ระบบจะคำนวณเลขรันถัดไปจาก Math.Max(config.StartNumber, maxExisting + 1) ตามที่มีอยู่จริงใน TeamCertificates เสมอ
            level.IsCertificateNumberGenerated = true;

            await _context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return _mapper.Map<List<TeamCertificateDto>>(newCerts);
        }
    }
}
