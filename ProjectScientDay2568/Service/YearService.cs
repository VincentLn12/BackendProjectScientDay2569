using ProjectScientDay2568.Models.Dtos;
using ProjectScientDay2568.Models.Dtos.Team;
using static ProjectScientDay2568.Utility.SD;

namespace ProjectScientDay2568.Service
{
    public class YearService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IWebHostEnvironment _env;

        public YearService(ApplicationDbContext context, IMapper mapper, IWebHostEnvironment env)
        {
            _context = context;
            _mapper = mapper;
            _env = env;
        }

        public async Task<List<YearDto>> GetAllYearsAsync(CancellationToken ct = default)
        {
            var years = await _context.Years
                .AsNoTracking()
                .OrderByDescending(p => p.YearID)
                .ToListAsync(ct);

            var dtos = _mapper.Map<List<YearDto>>(years);
            var yearIds = years.Select(y => y.YearID).ToList();

            var certs = await _context.TeamCertificates
                .Where(tc => yearIds.Contains(tc.Team.CompetitionLevel.CompetitionYear.YearID))
                .Select(tc => new {
                    YearID = tc.Team.CompetitionLevel.CompetitionYear.YearID,
                    tc.CertificateNumber
                })
                .ToListAsync(ct);

            var certsGrouped = certs
                .GroupBy(c => c.YearID)
                .ToDictionary(
                    g => g.Key,
                    g => {
                        int maxVal = 0;
                        int minVal = int.MaxValue;
                        foreach (var item in g)
                        {
                            if (string.IsNullOrWhiteSpace(item.CertificateNumber)) continue;
                            var slash = item.CertificateNumber.IndexOf('/');
                            if (slash <= 0) continue;
                            if (int.TryParse(item.CertificateNumber[..slash], out var n))
                            {
                                maxVal = Math.Max(maxVal, n);
                                minVal = Math.Min(minVal, n);
                            }
                        }
                        return new {
                            Min = minVal == int.MaxValue ? (int?)null : minVal,
                            Max = maxVal == 0 ? (int?)null : maxVal
                        };
                    }
                );

            foreach (var dto in dtos)
            {
                if (certsGrouped.TryGetValue(dto.YearID, out var val))
                {
                    dto.MinCertificateNumber = val.Min;
                    dto.MaxCertificateNumber = val.Max;
                }
            }

            return dtos;
        }

        public async Task<YearDto> GetYearByIdAsync(int id, CancellationToken ct = default)
        {
            var year = await _context.Years
                .AsNoTracking()
                .FirstOrDefaultAsync(y => y.YearID == id, ct);

            if (year == null)
                throw new KeyNotFoundException($"Year with ID {id} not found.");

            var dto = _mapper.Map<YearDto>(year);

            var certs = await _context.TeamCertificates
                .Where(tc => tc.Team.CompetitionLevel.CompetitionYear.YearID == id)
                .Select(tc => tc.CertificateNumber)
                .ToListAsync(ct);

            int maxVal = 0;
            int minVal = int.MaxValue;
            foreach (var no in certs)
            {
                if (string.IsNullOrWhiteSpace(no)) continue;
                var slash = no.IndexOf('/');
                if (slash <= 0) continue;
                if (int.TryParse(no[..slash], out var n))
                {
                    maxVal = Math.Max(maxVal, n);
                    minVal = Math.Min(minVal, n);
                }
            }

            if (maxVal > 0)
            {
                dto.MinCertificateNumber = minVal == int.MaxValue ? null : minVal;
                dto.MaxCertificateNumber = maxVal;
            }

            return dto;
        }

        public async Task<YearDto> CreateYearAsync(YearDto yearDto, CancellationToken ct = default)
        {
            if (yearDto == null)
                throw new ArgumentNullException(nameof(yearDto), "YearDto cannot be null.");

            var year = _mapper.Map<Year>(yearDto);

            await _context.Years.AddAsync(year, ct);
            await _context.SaveChangesAsync(ct);

            return _mapper.Map<YearDto>(year);
        }

        public async Task<YearDto> UpdateYearAsync(int id, YearDto yearDto, CancellationToken ct = default)
        {
            if (yearDto == null)
                throw new ArgumentNullException(nameof(yearDto), "YearDto cannot be null.");

            var yearToUpdate = await _context.Years.FirstOrDefaultAsync(y => y.YearID == id, ct);
            if (yearToUpdate == null)
                throw new KeyNotFoundException($"Year with ID {id} not found.");

            _mapper.Map(yearDto, yearToUpdate);

            await _context.SaveChangesAsync(ct);
            return _mapper.Map<YearDto>(yearToUpdate);
        }

        public async Task DeleteYearAsync(int id, CancellationToken ct = default)
        {
            var year = await _context.Years.FirstOrDefaultAsync(y => y.YearID == id, ct);
            if (year == null)
                throw new KeyNotFoundException("ไม่พบปี");

            // (ออปชัน) ลบไฟล์พื้นหลังถ้าต้องการ
            // await DeleteBackgroundFileIfExistsAsync(year.CertificateBackgroundPath, ct);

            _context.Years.Remove(year);
            await _context.SaveChangesAsync(ct);
        }
        public async Task<List<TeamCertificateDto>> GenerateTeamCertificatesNumber(int year, CancellationToken ct = default)
        {
            var config = await _context.CertificateConfigs
                .AsTracking()
                .FirstOrDefaultAsync(c => c.YearID == year, ct)
                ?? throw new InvalidOperationException("ยังไม่ได้ตั้งค่าเกียรติบัตรสำหรับปีนี้");

            var name = (config.CertificateName ?? string.Empty).Trim();

            await using var tx = await _context.Database.BeginTransactionAsync(ct);

            var runningNumber = config.StartNumber;

            var teamIdsInYear = await _context.Teams
                .Where(t => t.CompetitionLevel.CompetitionYear.Year.YearID == year)
                .Select(t => t.TeamID)
                .ToListAsync(ct);

            var existingCertificates = await _context.TeamCertificates
                .Where(tc => teamIdsInYear.Contains(tc.TeamID))
                .ToListAsync(ct);

            if (existingCertificates.Count > 0)
            {
                _context.TeamCertificates.RemoveRange(existingCertificates);
                await _context.SaveChangesAsync(ct);
            }

            var teams = await _context.Teams
                .Where(t => t.Status == TeamStatus.CheckedIn &&
                            t.CompetitionLevel.CompetitionYear.Year.YearID == year)
                .OrderBy(t => t.RewardTypeID)
                .Include(t => t.CompetitionLevel)
                    .ThenInclude(cl => cl.CompetitionYear)
                        .ThenInclude(cy => cy.Year)
                .ToListAsync(ct);

            var certificates = new List<TeamCertificate>(teams.Count);
            foreach (var team in teams)
            {
                var yearNumber = team.CompetitionLevel.CompetitionYear.Year.YearNumber;
                certificates.Add(new TeamCertificate
                {
                    TeamID = team.TeamID,
                    CertificateNumber = $"{runningNumber}/{yearNumber}",
                    CertificateName = name,
                    IssuedDate = DateTime.UtcNow
                });
                runningNumber++;
            }

            if (certificates.Count > 0)
                await _context.TeamCertificates.AddRangeAsync(certificates, ct);

            // ไม่แก้ไขค่า config.StartNumber เพื่อคงค่าเริ่มต้นที่ผู้ใช้ตั้งไว้ (เช่น 50)
            await _context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return _mapper.Map<List<TeamCertificateDto>>(certificates);
        }  
        public async Task<YearDto> CloneYearAsync(CloneYearRequest req, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(req.NewYearName))
                throw new ArgumentException("NewYearName ห้ามว่าง", nameof(req.NewYearName));

            var srcYear = await _context.Years
                .AsNoTracking()
                .Include(y => y.CertificateConfig)
                .FirstOrDefaultAsync(y => y.YearID == req.SourceYearId, ct);

            if (srcYear == null)
                throw new KeyNotFoundException($"ไม่พบปีต้นฉบับ (YearID={req.SourceYearId})");

            var duplicate = await _context.Years.AnyAsync(y => y.YearNumber == req.NewYearNumber, ct);
            if (duplicate)
                throw new InvalidOperationException($"มี YearNumber={req.NewYearNumber} อยู่แล้ว");

            string oldYearStr = srcYear.YearNumber.ToString();
            string newYearStr = req.NewYearNumber.ToString();
            int diffYears = req.NewYearNumber - srcYear.YearNumber;

            string ReplaceYearInText(string? text)
            {
                if (string.IsNullOrWhiteSpace(text)) return text ?? string.Empty;
                return text.Replace(oldYearStr, newYearStr);
            }

            DateTime? ShiftYearInDate(DateTime? dt)
            {
                if (!dt.HasValue || diffYears == 0) return dt;
                try { return dt.Value.AddYears(diffYears); } catch { return dt; }
            }

            await using var tx = await _context.Database.BeginTransactionAsync(ct);

            // 1) สร้าง Year ใหม่
            var newYear = new Year
            {
                YearNumber = req.NewYearNumber,
                YearName = ReplaceYearInText(req.NewYearName.Trim()),
                isActive = false,
                CompetitionRegulationsUrl = srcYear.CompetitionRegulationsUrl,
                DateName = ReplaceYearInText(srcYear.DateName),
                RegistrationOpenDate = ShiftYearInDate(srcYear.RegistrationOpenDate),
                RegistrationCloseDate = ShiftYearInDate(srcYear.RegistrationCloseDate)
            };
            await _context.Years.AddAsync(newYear, ct);
            await _context.SaveChangesAsync(ct); // เพื่อให้ได้ newYear.YearID

            // 2) เตรียม/คัดลอก CertificateConfig
            CertificateConfig? newCfg = null;
            if (req.CopyCertificateConfig && srcYear.CertificateConfig != null)
            {
                newCfg = new CertificateConfig
                {
                    YearID = newYear.YearID,
                    CertificateName = ReplaceYearInText(srcYear.CertificateConfig.CertificateName),
                    StartNumber = req.ResetCertificateStartNumberToOne
                        ? 1
                        : srcYear.CertificateConfig.StartNumber,

                    // ถ้ามีฟิลด์ชื่อ/ตำแหน่ง/ลายเซ็น ให้คัดลอกด้วย
                    UniversityDeanTitle = ReplaceYearInText(srcYear.CertificateConfig.UniversityDeanTitle),
                    UniversityDeanName = ReplaceYearInText(srcYear.CertificateConfig.UniversityDeanName),
                    UniversityDeanSignature = srcYear.CertificateConfig.UniversityDeanSignature,
                    FacultyDeanTitle = ReplaceYearInText(srcYear.CertificateConfig.FacultyDeanTitle),
                    FacultyDeanName = ReplaceYearInText(srcYear.CertificateConfig.FacultyDeanName),
                    FacultyDeanSignature = srcYear.CertificateConfig.FacultyDeanSignature,
                    UniversityLogoPath = srcYear.CertificateConfig.UniversityLogoPath,
                    InstituteLogoPath = srcYear.CertificateConfig.InstituteLogoPath,
                    CertificateBackgroundPath = null // จะกำหนดด้านล่างถ้าขอคัดลอกไฟล์
                };
                await _context.CertificateConfigs.AddAsync(newCfg, ct);
            }
            else
            {
                // ไม่คัดลอก config แต่ต้องมีเรคอร์ดไว้เก็บพื้นหลังใหม่/ภายหลัง
                newCfg = new CertificateConfig { YearID = newYear.YearID };
                await _context.CertificateConfigs.AddAsync(newCfg, ct);
            }

            await _context.SaveChangesAsync(ct);

            // 3) คัดลอกไฟล์พื้นหลัง (ถ้ามีและถ้าขอ)
            if (req.CopyCertificateBackgroundFile
                && srcYear.CertificateConfig?.CertificateBackgroundPath is string srcBgWebPath
                && !string.IsNullOrWhiteSpace(srcBgWebPath))
            {
                var copiedWebPath = await CopyYearBackgroundFileAsync(
                    srcBgWebPath, srcYear.YearID, newYear.YearID, ct);

                newCfg!.CertificateBackgroundPath = copiedWebPath;
                await _context.SaveChangesAsync(ct);
            }

            // 4) คัดลอก CompetitionYear → CompetitionLevel → Awards (ไม่คัดลอกทีม/แมทช์)
            if (req.CopyCompetitionYears)
            {
                var srcCompYears = await _context.CompetitionYears
                    .AsNoTracking()
                    .Where(cy => cy.YearID == srcYear.YearID)
                    .ToListAsync(ct);

                foreach (var scy in srcCompYears)
                {
                    var ncy = new CompetitionYear
                    {
                        YearID = newYear.YearID,
                        CompetitionID = scy.CompetitionID,
                        RegistrationLocation = ReplaceYearInText(scy.RegistrationLocation),
                    };
                    await _context.CompetitionYears.AddAsync(ncy, ct);
                    await _context.SaveChangesAsync(ct); // ต้องการ ncy.CompetitionYearID สำหรับลูก

                    if (req.CopyCompetitionLevels)
                    {
                        var srcLevels = await _context.CompetitionLevels
                            .AsNoTracking()
                            .Where(cl => cl.CompetitionYearID == scy.CompetitionYearID)
                            .ToListAsync(ct);

                        foreach (var sl in srcLevels)
                        {
                            var nl = new CompetitionLevel
                            {
                                CompetitionYearID = ncy.CompetitionYearID,
                                LevelID = sl.LevelID,
                                CompetitionDateTime = ShiftYearInDate(sl.CompetitionDateTime) ?? sl.CompetitionDateTime,
                                MainPlayersCount = sl.MainPlayersCount,
                                ReservePlayersCount = sl.ReservePlayersCount
                            };
                            await _context.CompetitionLevels.AddAsync(nl, ct);
                            await _context.SaveChangesAsync(ct); // ต้องการ nl.CompetitionLevelID
                        }
                    }
                }
            }
            await tx.CommitAsync(ct);

            // คืนค่า DTO
            return _mapper.Map<YearDto>(newYear);
        }
        private async Task<string?> CopyYearBackgroundFileAsync(
             string srcWebPath, int srcYearId, int newYearId, CancellationToken ct)
        {
            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var srcPhysical = Path.Combine(webRoot, srcWebPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

            if (!File.Exists(srcPhysical))
                return null;

            var ext = Path.GetExtension(srcPhysical);
            var newDir = Path.Combine(webRoot, "uploads", "years", newYearId.ToString());
            Directory.CreateDirectory(newDir);

            var newFileName = "certificate-bg" + ext.ToLowerInvariant();
            var newPhysical = Path.Combine(newDir, newFileName);

            using (var src = new FileStream(srcPhysical, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var dst = new FileStream(newPhysical, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await src.CopyToAsync(dst, ct);
            }

            var newWebPath = $"/uploads/years/{newYearId}/{newFileName}";
            return newWebPath;
        }
     
    }
}
