using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text.RegularExpressions;
using ProjectScientDay2568.Utility;
using QuestPDF.Drawing;
using static ProjectScientDay2568.Utility.SD;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Globalization;
using PageSize = DocumentFormat.OpenXml.Wordprocessing.PageSize;
namespace ProjectScientDay2568.Service
{
    public class ExportService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly YearService _yearService;
        private readonly CertificateConfigService _configService;

        public ExportService(ApplicationDbContext context, IWebHostEnvironment env, YearService yearService, CertificateConfigService configService)
        {
            _context = context;
            _env = env;
            _yearService = yearService;
            _configService = configService;
        }

        private static bool _isFontRegistered = false;
        private static readonly object _fontLock = new object();

        private void EnsureFontRegistered()
        {
            if (_isFontRegistered) return;

            lock (_fontLock)
            {
                if (_isFontRegistered) return;
                try
                {
                    var candidates = new List<string>();

                    if (!string.IsNullOrEmpty(_env.ContentRootPath))
                        candidates.Add(Path.Combine(_env.ContentRootPath, "Fonts", "THSarabunNew.ttf"));

                    if (!string.IsNullOrEmpty(AppContext.BaseDirectory))
                        candidates.Add(Path.Combine(AppContext.BaseDirectory, "Fonts", "THSarabunNew.ttf"));

                    if (!string.IsNullOrEmpty(_env.WebRootPath))
                        candidates.Add(Path.Combine(_env.WebRootPath, "Fonts", "THSarabunNew.ttf"));

                    candidates.Add(Path.Combine(Directory.GetCurrentDirectory(), "Fonts", "THSarabunNew.ttf"));

                    foreach (var fontPath in candidates)
                    {
                        if (File.Exists(fontPath))
                        {
                            using var fontStream = File.OpenRead(fontPath);
                            FontManager.RegisterFont(fontStream);
                            _isFontRegistered = true;
                            break;
                        }
                    }
                }
                catch
                {
                    _isFontRegistered = true;
                }
            }
        }

        public async Task<byte[]> GenerateCertificateTeamAsync(int teamId)
        {
            // ===== 1) โหลดข้อมูลทีม + คอนฟิกของปี =====
            var team = await _context.Teams
                .Include(t => t.RewardType)
                .Include(t => t.Students)
                .Include(t => t.CompetitionLevel).ThenInclude(cl => cl.CompetitionYear).ThenInclude(cy => cy.Competition)
                .Include(t => t.CompetitionLevel).ThenInclude(cl => cl.CompetitionYear).ThenInclude(p => p.Year)
                .Include(t => t.CompetitionLevel).ThenInclude(cl => cl.EducationLevel)
                .Include(t => t.TeamCertificates)
                .Include(t => t.schools)
                .FirstOrDefaultAsync(t => t.TeamID == teamId);

            if (team == null)
                throw new KeyNotFoundException($"Team with ID {teamId} not found.");

            string PrefixToString(SD.TitlePrefix prefix)
                => prefix == SD.TitlePrefix.Other ? "" : prefix.ToString();

            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            EnsureFontRegistered();

            var yearId = team.CompetitionLevel?.CompetitionYear?.Year?.YearID ?? 0;
            var config = (yearId > 0) ? await _configService.GetCertificateConfigAsync(yearId) : null;

            // ===== 2) โหลดรูปพื้นหลัง =====
            byte[]? backgroundImage = null;
            if (yearId > 0)
                backgroundImage = await _configService.GetCertificateBackgroundBytesAsync(yearId);

            if (backgroundImage == null)
            {
                var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var defaultBg = Path.Combine(webRoot, "certificate-bg.png");
                if (File.Exists(defaultBg))
                    backgroundImage = await File.ReadAllBytesAsync(defaultBg);
            }

            // ===== 2.1) โหลดลายเซ็น + ทำพื้นหลังขาวโปร่งใส =====
            var leftSignatureRaw = await LoadImageBytesAsync(config?.UniversityDeanSignature);
            var rightSignatureRaw = await LoadImageBytesAsync(config?.FacultyDeanSignature);
            var leftSignature = await MakeSignatureTransparentAsync(leftSignatureRaw);
            var rightSignature = await MakeSignatureTransparentAsync(rightSignatureRaw);

            // ===== 2.2) โหลดโลโก้ =====
            var uniLogo = await LoadImageBytesAsync(config?.UniversityLogoPath);
            var instLogo = await LoadImageBytesAsync(config?.InstituteLogoPath);

            // ===== 3) เตรียมข้อความ =====
            string certificateNo = "";
            if (team.TeamCertificates?.Any() == true)
            {
                var numbers = team.TeamCertificates
                    .Where(s => !string.IsNullOrWhiteSpace(s.CertificateNumber))
                    .Select(s => s.CertificateNumber!.Trim());

                if (numbers.Any())
                    certificateNo = "เลขที่ " + string.Join(" ", numbers);
            }


            var title = "เกียรติบัตรฉบับนี้ให้ไว้เพื่อแสดงว่า";

            // === Helpers ===
            static string CleanToken(string? s, bool removeInnerSpaces = false)
            {
                var t = (s ?? string.Empty)
                    .Replace('\u00A0', ' ')
                    .Replace("\u200B", "");
                t = t.Trim();
                if (removeInnerSpaces) t = System.Text.RegularExpressions.Regex.Replace(t, @"\s+", "");
                return t;
            }

            string SafeName(SD.TitlePrefix prefix, string? first, string? last)
            {
                var prefixStr = CleanToken(PrefixToString(prefix), removeInnerSpaces: true);
                var f = CleanToken(first, removeInnerSpaces: true);
                var l = CleanToken(last, removeInnerSpaces: false);

                if (string.IsNullOrEmpty(f) && string.IsNullOrEmpty(l))
                    return "";

                var fullFirst = prefixStr + f;
                return string.Join(" ", new[] { fullFirst, l }.Where(x => !string.IsNullOrWhiteSpace(x)));
            }

            var studentNameList = team.Students
                .Select(s => SafeName(s.TitlePrefix, s.FirstName, s.LastName))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            static IEnumerable<IEnumerable<string>> ChunkBy2(IEnumerable<string> source)
            {
                using var e = source.GetEnumerator();
                while (true)
                {
                    var buf = new List<string>(3);
                    for (int i = 0; i < 3 && e.MoveNext(); i++) buf.Add(e.Current);
                    if (buf.Count == 0) yield break;
                    yield return buf;
                }
            }

            var studentLines = ChunkBy2(studentNameList)
                .Select(group => string.Join("  ", group))
                .ToList();

            var teacherPrimary = SafeName(team.PrimaryTitlePrefix, team.PrimaryFirstName, team.PrimaryLastName);
            var teacherSecondary = SafeName(team.SecondaryTitlePrefix, team.SecondaryFirstName, team.SecondaryLastName);
            var teacherLine = ("ครูผู้ฝึกสอน " + string.Join("  ",
                new[] { teacherPrimary, teacherSecondary }.Where(x => !string.IsNullOrWhiteSpace(x)))).Trim();

            var displaySchoolName = team.schools?.school_name ?? "";

            string rewardLine =
                team.RewardType != null
                ? $"{team.RewardType.RewardTypeName} {team.CompetitionLevel!.CompetitionYear!.Competition!.CompetitionName} {team.CompetitionLevel!.EducationLevel!.LevelName}"
                : $" {team.CompetitionLevel!.CompetitionYear!.Competition!.CompetitionName} {team.CompetitionLevel!.EducationLevel!.LevelName}";

            var yearNumber = team.CompetitionLevel!.CompetitionYear!.Year!.YearNumber;
            var dateName = team.CompetitionLevel!.CompetitionYear!.Year!.DateName;
            var lineEvent1 = $"งานสัปดาห์วิทยาศาสตร์แห่งชาติ (ส่วนภูมิภาค) ประจำปี {yearNumber}";
            var lineEvent2 = $"ระหว่างวันที่ {dateName} {yearNumber} ณ มหาวิทยาลัยราชภัฏกาญจนบุรี";
            var blessing = "ขออวยพรให้ประสบความสุข ความเจริญก้าวหน้าตลอดไป";

            var leftName = string.IsNullOrWhiteSpace(config?.UniversityDeanName) ? " " : config!.UniversityDeanName!;
            var rightName = string.IsNullOrWhiteSpace(config?.FacultyDeanName) ? " " : config!.FacultyDeanName!;
            var leftRole = string.IsNullOrWhiteSpace(config?.UniversityDeanTitle) ? "อธิการบดีมหาวิทยาลัย" : config!.UniversityDeanTitle!;
            var rightRole = string.IsNullOrWhiteSpace(config?.FacultyDeanTitle) ? "คณบดีคณะวิทยาศาสตร์และเทคโนโลยี" : config!.FacultyDeanTitle!;

            // ===== 4) สร้าง PDF =====
            using var stream = new MemoryStream();

            QuestPDF.Fluent.Document.Create(doc =>
            {
                doc.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(0);

                    page.DefaultTextStyle(x => x
                        .FontFamily("TH Sarabun New")
                        .FontSize(22)
                        .LineHeight(1.05f)
                    );

                    if (backgroundImage != null)
                        page.Background().Image(backgroundImage, ImageScaling.FitArea);


                    if (!string.IsNullOrWhiteSpace(certificateNo))
                    {
                        page.Foreground().Element(c =>
                            c.AlignTop().AlignRight().PaddingRight(70).PaddingTop(60)
                             .Text(certificateNo)
                             .FontSize(16).LineHeight(1.0f)
                        );
                    }

                    // Content
                    page.Content()
                        .PaddingHorizontal(60)
                        .PaddingTop(62)
                        .AlignTop()
                        .AlignCenter()
                        .Column(col =>
                        {
                            col.Spacing(2);

                            // โลโก้ 2 อันเหนือหัวเรื่อง
                            col.Item().AlignCenter().Row(r =>
                            {
                                r.Spacing(16);

                                // โลโก้มหาวิทยาลัย (ใหญ่กว่า)
                                r.AutoItem().Element(LogoCellUni).Element(e =>
                                {
                                    if (uniLogo != null) e.Image(uniLogo, ImageScaling.FitArea);
                                    else e.Text(" ");
                                });

                                // โลโก้สถาบัน (เล็กกว่า)
                                r.AutoItem().Element(LogoCellInst).Element(e =>
                                {
                                    if (instLogo != null) e.Image(instLogo, ImageScaling.FitArea);
                                    else e.Text(" ");
                                });

                                // โลโก้มหาวิทยาลัย
                                IContainer LogoCellUni(IContainer c) =>
                                    c.Width(80).Height(80).AlignCenter().AlignMiddle().Padding(2);

                                // โลโก้สถาบัน
                                IContainer LogoCellInst(IContainer c) =>
                                    c.Width(80).Height(80).AlignCenter().AlignMiddle().Padding(2);
                            });

                            col.Item().Height(5);
                            // หัวเรื่อง
                            col.Item().AlignCenter().Text(title).FontSize(30).SemiBold().LineHeight(1.0f);

                            col.Item().Height(5);

                            foreach (var line in studentLines)
                                col.Item().AlignCenter().Text(line).FontSize(22).LineHeight(1.2f);

                            if (!string.IsNullOrWhiteSpace(teacherLine))
                            {
                                col.Item().AlignCenter().Text(t =>
                                {
                                    // ตัวหนาเฉพาะ "ครูผู้ฝึกสอน"
                                    t.Span("ครูผู้ฝึกสอน ").Bold().FontSize(22).LineHeight(1.0f);

                                    // ต่อด้วยรายชื่อครู
                                    var teacherNames = string.Join("  ",
                                        new[] { teacherPrimary, teacherSecondary }
                                        .Where(x => !string.IsNullOrWhiteSpace(x)));

                                    t.Span(teacherNames).FontSize(22).LineHeight(1.0f);
                                });
                            }


                            if (!string.IsNullOrWhiteSpace(displaySchoolName))
                                col.Item().AlignCenter().Text(displaySchoolName).FontSize(22).SemiBold().LineHeight(1.0f);

                            col.Item().AlignCenter().Text(t =>
                            {
                                if (team.RewardType != null)
                                {
                                    // ทำให้ RewardTypeName เป็นตัวหนา + ขนาดฟอนต์
                                    t.Span(team.RewardType.RewardTypeName)
                                        .Bold()
                                        .FontSize(22)
                                        .LineHeight(1.0f);

                                    // ต่อด้วยข้อความอื่นปกติ
                                    t.Span($" {team.CompetitionLevel!.CompetitionYear!.Competition!.CompetitionName} {team.CompetitionLevel!.EducationLevel!.LevelName}")
                                        .FontSize(22)
                                        .LineHeight(1.0f);
                                }
                                else
                                {
                                    t.Span($" {team.CompetitionLevel!.CompetitionYear!.Competition!.CompetitionName} {team.CompetitionLevel!.EducationLevel!.LevelName}")
                                        .FontSize(22)
                                        .LineHeight(1.0f);
                                }
                            });

                            col.Item().Height(5);

                            col.Item().AlignCenter().Text(lineEvent1).FontSize(26).SemiBold().LineHeight(1.5f);
                            col.Item().AlignCenter().Text(lineEvent2).FontSize(26).SemiBold().LineHeight(1.0f);
                            col.Item().AlignCenter().Text(blessing).FontSize(24).SemiBold().LineHeight(1.5f);

                            col.Item().Height(20);

                            // ลายเซ็น
                            col.Item().AlignCenter().Width(700).PaddingHorizontal(12).Table(t =>
                            {
                                t.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(1);
                                    c.RelativeColumn(1);
                                });

                                t.Cell().Row(1).Column(1).AlignCenter().Element(SignCell).Element(e =>
                                {
                                    if (leftSignature != null) e.Image(leftSignature, ImageScaling.FitArea);
                                    else e.Text(" ");
                                });

                                t.Cell().Row(1).Column(2).AlignCenter().Element(SignCellRight).Element(e =>
                                {
                                    if (rightSignature != null) e.Image(rightSignature, ImageScaling.FitArea);
                                    else e.Text(" ");
                                });


                                t.Cell().Row(2).Column(1).AlignCenter().Text(NoWrap(leftName)).FontSize(18).Bold().LineHeight(1.0f);
                                t.Cell().Row(2).Column(2).AlignCenter().Text(NoWrap(rightName)).FontSize(18).Bold().LineHeight(1.0f);

                                t.Cell().Row(3).Column(1).AlignCenter().Text(leftRole).FontSize(17).LineHeight(1.0f);
                                t.Cell().Row(3).Column(2).AlignCenter().Text(rightRole).FontSize(17).LineHeight(1.0f);

                                IContainer SignCell(IContainer c) =>
                                    c.Width(100).Height(36).AlignCenter().AlignBottom().PaddingHorizontal(2);

                                IContainer SignCellRight(IContainer c) =>
                                    c.Width(100).Height(36).AlignCenter().AlignBottom().PaddingHorizontal(2);
                            });
                        });
                });
            }).GeneratePdf(stream);

            return stream.ToArray();

            // ===== Helper =====
            async Task<byte[]?> LoadImageBytesAsync(string? pathOrUrl)
            {
                if (string.IsNullOrWhiteSpace(pathOrUrl)) return null;

                if (pathOrUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        using var http = new HttpClient();
                        return await http.GetByteArrayAsync(pathOrUrl);
                    }
                    catch { return null; }
                }

                var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var normalized = pathOrUrl.TrimStart('~').TrimStart('/');
                var full = Path.Combine(webRoot, normalized);
                if (File.Exists(full)) return await File.ReadAllBytesAsync(full);

                var uploads = Path.Combine(webRoot, "uploads", normalized);
                if (File.Exists(uploads)) return await File.ReadAllBytesAsync(uploads);

                return null;
            }

            static string NoWrap(string? s)
            {
                if (string.IsNullOrWhiteSpace(s)) return string.Empty;
                return System.Text.RegularExpressions.Regex.Replace(s.Trim(), @"\s+", "\u00A0");
            }
        }
        public async Task<byte[]> GenerateCertificateStudentAsync(int teamId, int studentId)
        {
            var team = await _context.Teams
                .Include(t => t.RewardType)
                .Include(p => p.Students).ThenInclude(s => s.RewardType)
                .Include(t => t.CompetitionLevel).ThenInclude(cl => cl.CompetitionYear).ThenInclude(cy => cy.Competition)
                .Include(t => t.CompetitionLevel).ThenInclude(cl => cl.CompetitionYear).ThenInclude(p => p.Year).ThenInclude(y => y.CertificateConfig)
                .Include(t => t.CompetitionLevel).ThenInclude(cl => cl.EducationLevel)
                .Include(p => p.schools)
                .FirstOrDefaultAsync(t => t.TeamID == teamId);

            if (team == null)
                throw new KeyNotFoundException($"Team with ID {teamId} not found.");

            var student = team.Students.FirstOrDefault(s => s.StudentID == studentId);
            if (student == null)
                throw new KeyNotFoundException($"Student with ID {studentId} not found in the team.");

            // ผู้สมัครแบบเดี่ยวถูกบันทึกเป็นทีมที่มีสมาชิกหนึ่งคน จึงใช้เลขรัน
            // ของ TeamCertificate เดียวกับที่สร้างเมื่อปิดกิจกรรม
            var certificateNo = await _context.TeamCertificates
                .Where(c => c.TeamID == teamId && !string.IsNullOrWhiteSpace(c.CertificateNumber))
                .OrderBy(c => c.CertificateID)
                .Select(c => c.CertificateNumber!.Trim())
                .FirstOrDefaultAsync();

            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            EnsureFontRegistered();

            var yearId = team.CompetitionLevel?.CompetitionYear?.Year?.YearID ?? 0;
            var config = yearId > 0 ? await _configService.GetCertificateConfigAsync(yearId) : null;
            byte[]? backgroundImage = yearId > 0
                ? await _configService.GetCertificateBackgroundBytesAsync(yearId)
                : null;

            if (backgroundImage == null)
            {
                var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var defaultBg = Path.Combine(webRoot, "certificate-bg.png");
                if (File.Exists(defaultBg))
                    backgroundImage = await File.ReadAllBytesAsync(defaultBg);
            }

            var leftSignature = await MakeSignatureTransparentAsync(await LoadImageBytesAsync(config?.UniversityDeanSignature));
            var rightSignature = await MakeSignatureTransparentAsync(await LoadImageBytesAsync(config?.FacultyDeanSignature));
            var uniLogo = await LoadImageBytesAsync(config?.UniversityLogoPath);
            var instLogo = await LoadImageBytesAsync(config?.InstituteLogoPath);

            string PrefixToString(SD.TitlePrefix prefix) => prefix == SD.TitlePrefix.Other ? "" : prefix.ToString();
            static string CleanToken(string? value, bool removeInnerSpaces = false)
            {
                var text = (value ?? string.Empty).Replace('\u00A0', ' ').Replace("\u200B", "").Trim();
                return removeInnerSpaces ? System.Text.RegularExpressions.Regex.Replace(text, @"\s+", "") : text;
            }
            string SafeName(SD.TitlePrefix prefix, string? first, string? last)
            {
                var firstPart = CleanToken(PrefixToString(prefix), true) + CleanToken(first, true);
                var lastPart = CleanToken(last);
                return string.Join(" ", new[] { firstPart, lastPart }.Where(x => !string.IsNullOrWhiteSpace(x)));
            }
            static string NoWrap(string? value) => string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : System.Text.RegularExpressions.Regex.Replace(value.Trim(), @"\s+", "\u00A0");

            var studentName = SafeName(student.TitlePrefix, student.FirstName, student.LastName);
            var primaryAdvisor = SafeName(team.PrimaryTitlePrefix, team.PrimaryFirstName, team.PrimaryLastName);
            var secondaryAdvisor = SafeName(team.SecondaryTitlePrefix, team.SecondaryFirstName, team.SecondaryLastName);
            var advisorNames = string.Join("  ", new[] { primaryAdvisor, secondaryAdvisor }.Where(x => !string.IsNullOrWhiteSpace(x)));
            var competitionName = team.CompetitionLevel!.CompetitionYear!.Competition!.CompetitionName;
            var levelName = team.CompetitionLevel.EducationLevel!.LevelName;
            var schoolName = team.schools?.school_name ?? string.Empty;
            var year = team.CompetitionLevel.CompetitionYear.Year!;
            var leftName = string.IsNullOrWhiteSpace(config?.UniversityDeanName) ? " " : config!.UniversityDeanName!;
            var rightName = string.IsNullOrWhiteSpace(config?.FacultyDeanName) ? " " : config!.FacultyDeanName!;
            var leftRole = string.IsNullOrWhiteSpace(config?.UniversityDeanTitle) ? "อธิการบดีมหาวิทยาลัย" : config!.UniversityDeanTitle!;
            var rightRole = string.IsNullOrWhiteSpace(config?.FacultyDeanTitle) ? "คณบดีคณะวิทยาศาสตร์และเทคโนโลยี" : config!.FacultyDeanTitle!;

            using var stream = new MemoryStream();

            QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(0);
                    page.DefaultTextStyle(x => x.FontFamily("TH Sarabun New").FontSize(22).LineHeight(1.05f));

                    if (backgroundImage != null)
                        page.Background().Image(backgroundImage, ImageScaling.FitArea);

                    if (!string.IsNullOrWhiteSpace(certificateNo))
                    {
                        page.Foreground().Element(c =>
                            c.AlignTop().AlignRight().PaddingRight(70).PaddingTop(60)
                             .Text($"เลขที่ {certificateNo}")
                             .FontSize(16).LineHeight(1.0f)
                        );
                    }

                    page.Content().PaddingHorizontal(60).PaddingTop(62).AlignTop().AlignCenter().Column(column =>
                    {
                        column.Spacing(2);

                        column.Item().AlignCenter().Row(row =>
                        {
                            row.Spacing(16);
                            row.AutoItem().Width(80).Height(80).AlignCenter().AlignMiddle().Padding(2).Element(e =>
                            {
                                if (uniLogo != null) e.Image(uniLogo, ImageScaling.FitArea); else e.Text(" ");
                            });
                            row.AutoItem().Width(80).Height(80).AlignCenter().AlignMiddle().Padding(2).Element(e =>
                            {
                                if (instLogo != null) e.Image(instLogo, ImageScaling.FitArea); else e.Text(" ");
                            });
                        });

                        column.Item().Height(5);
                        column.Item().AlignCenter().Text("เกียรติบัตรฉบับนี้ให้ไว้เพื่อแสดงว่า").FontSize(30).SemiBold().LineHeight(1.0f);
                        column.Item().Height(5);
                        column.Item().AlignCenter().Text(studentName).FontSize(22).LineHeight(1.2f);

                        if (!string.IsNullOrWhiteSpace(advisorNames))
                            column.Item().AlignCenter().Text(t =>
                            {
                                t.Span("ครูผู้ฝึกสอน ").Bold().FontSize(22).LineHeight(1.0f);
                                t.Span(advisorNames).FontSize(22).LineHeight(1.0f);
                            });

                        if (!string.IsNullOrWhiteSpace(schoolName))
                            column.Item().AlignCenter().Text(schoolName).FontSize(22).SemiBold().LineHeight(1.0f);

                        column.Item().AlignCenter().Text(t =>
                        {
                            if (student.RewardType != null)
                                t.Span(student.RewardType.RewardTypeName).Bold().FontSize(22).LineHeight(1.0f);
                            t.Span($" {competitionName} {levelName}").FontSize(22).LineHeight(1.0f);
                        });

                        column.Item().Height(5);
                        column.Item().AlignCenter().Text($"งานสัปดาห์วิทยาศาสตร์แห่งชาติ (ส่วนภูมิภาค) ประจำปี {year.YearNumber}").FontSize(26).SemiBold().LineHeight(1.5f);
                        column.Item().AlignCenter().Text($"ระหว่างวันที่ {year.DateName} {year.YearNumber} ณ มหาวิทยาลัยราชภัฏกาญจนบุรี").FontSize(26).SemiBold().LineHeight(1.0f);
                        column.Item().AlignCenter().Text("ขออวยพรให้ประสบความสุข ความเจริญก้าวหน้าตลอดไป").FontSize(24).SemiBold().LineHeight(1.5f);
                        column.Item().Height(20);

                        column.Item().AlignCenter().Width(700).PaddingHorizontal(12).Table(table =>
                        {
                            table.ColumnsDefinition(columns => { columns.RelativeColumn(1); columns.RelativeColumn(1); });
                            table.Cell().Row(1).Column(1).AlignCenter().Width(100).Height(36).AlignBottom().Element(e => { if (leftSignature != null) e.Image(leftSignature, ImageScaling.FitArea); else e.Text(" "); });
                            table.Cell().Row(1).Column(2).AlignCenter().Width(100).Height(36).AlignBottom().Element(e => { if (rightSignature != null) e.Image(rightSignature, ImageScaling.FitArea); else e.Text(" "); });
                            table.Cell().Row(2).Column(1).AlignCenter().Text(NoWrap(leftName)).FontSize(18).Bold().LineHeight(1.0f);
                            table.Cell().Row(2).Column(2).AlignCenter().Text(NoWrap(rightName)).FontSize(18).Bold().LineHeight(1.0f);
                            table.Cell().Row(3).Column(1).AlignCenter().Text(leftRole).FontSize(17).LineHeight(1.0f);
                            table.Cell().Row(3).Column(2).AlignCenter().Text(rightRole).FontSize(17).LineHeight(1.0f);
                        });
                    });
                });
            }).GeneratePdf(stream);

            return stream.ToArray();

            async Task<byte[]?> LoadImageBytesAsync(string? pathOrUrl)
            {
                if (string.IsNullOrWhiteSpace(pathOrUrl)) return null;
                if (pathOrUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    try { using var http = new HttpClient(); return await http.GetByteArrayAsync(pathOrUrl); }
                    catch { return null; }
                }

                var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var normalized = pathOrUrl.TrimStart('~').TrimStart('/');
                var full = Path.Combine(webRoot, normalized);
                if (File.Exists(full)) return await File.ReadAllBytesAsync(full);
                var uploads = Path.Combine(webRoot, "uploads", normalized);
                return File.Exists(uploads) ? await File.ReadAllBytesAsync(uploads) : null;
            }
        }
        // ===== เกียรติบัตรผู้เข้าอบรม: ใช้พื้นหลัง/โลโก้/ลายเซ็นจากการตั้งค่าเกียรติบัตรของปี (CertificateConfig) =====

        // เกียรติบัตร 1 ใบ; ownerUserId != null → ต้องเป็นการสมัครของผู้ใช้คนนั้น
        public async Task<byte[]> GenerateTrainingCertificateAsync(int registrationId, string? ownerUserId)
        {
            var reg = await _context.TrainingRegistrations.AsNoTracking()
                .Include(r => r.Trainingtopic)
                .FirstOrDefaultAsync(r => r.Id == registrationId)
                ?? throw new KeyNotFoundException("ไม่พบข้อมูลการสมัคร");

            if (ownerUserId != null && reg.UserId != ownerUserId)
                throw new UnauthorizedAccessException("ไม่มีสิทธิ์ดาวน์โหลดเกียรติบัตรนี้");
            if (reg.Status == "ยกเลิกแล้ว")
                throw new InvalidOperationException("การสมัครนี้ถูกยกเลิกแล้ว จึงไม่มีเกียรติบัตร");

            return await BuildTrainingCertificatesAsync(reg.Trainingtopic!, new List<string> { TrainingFullName(reg) });
        }

        // เกียรติบัตรทุกคนในหลักสูตร ไฟล์เดียว คนละหน้า (ไม่รวมผู้ที่ยกเลิก)
        public async Task<byte[]> GenerateTrainingCertificatesForTopicAsync(int trainingtopicId)
        {
            var topic = await _context.Trainingtopics.AsNoTracking().FirstOrDefaultAsync(t => t.Id == trainingtopicId)
                ?? throw new KeyNotFoundException("ไม่พบหลักสูตรอบรม");

            var regs = await _context.TrainingRegistrations.AsNoTracking()
                .Where(r => r.TrainingtopicId == trainingtopicId && r.Status != "ยกเลิกแล้ว")
                .OrderBy(r => r.RegisteredAt)
                .ToListAsync();
            if (regs.Count == 0)
                throw new KeyNotFoundException("ยังไม่มีผู้ลงทะเบียนในหลักสูตรนี้");

            return await BuildTrainingCertificatesAsync(topic, regs.Select(TrainingFullName).ToList());
        }

        private static string TrainingFullName(TrainingRegistration r) =>
            System.Text.RegularExpressions.Regex.Replace($"{r.Prefix}{r.FirstName} {r.LastName}".Replace('\u00A0', ' '), @"\s+", " ").Trim();

        private async Task<byte[]> BuildTrainingCertificatesAsync(Trainingtopic topic, List<string> names)
        {
            if (topic.YearID is not int yearId)
                throw new InvalidOperationException("หลักสูตรนี้ยังไม่ได้ระบุปีการจัดงาน กรุณาแก้ไขหลักสูตรแล้วเลือกปี");

            var year = await _context.Years.AsNoTracking().FirstOrDefaultAsync(y => y.YearID == yearId)
                ?? throw new KeyNotFoundException("ไม่พบปีการจัดงานของหลักสูตรนี้");
            var config = await _configService.GetCertificateConfigAsync(yearId);

            var background = await _configService.GetCertificateBackgroundBytesAsync(yearId)
                ?? await LoadWebImageAsync("certificate-bg.png");
            var leftSignature = await MakeSignatureTransparentAsync(await LoadWebImageAsync(config?.UniversityDeanSignature));
            var rightSignature = await MakeSignatureTransparentAsync(await LoadWebImageAsync(config?.FacultyDeanSignature));
            var uniLogo = await LoadWebImageAsync(config?.UniversityLogoPath);
            var instLogo = await LoadWebImageAsync(config?.InstituteLogoPath);

            static string NoWrap(string? v) => string.IsNullOrWhiteSpace(v) ? " " : System.Text.RegularExpressions.Regex.Replace(v.Trim(), @"\s+", "\u00A0");
            var leftName = NoWrap(config?.UniversityDeanName);
            var rightName = NoWrap(config?.FacultyDeanName);
            var leftRole = string.IsNullOrWhiteSpace(config?.UniversityDeanTitle) ? "อธิการบดีมหาวิทยาลัย" : config!.UniversityDeanTitle!;
            var rightRole = string.IsNullOrWhiteSpace(config?.FacultyDeanTitle) ? "คณบดีคณะวิทยาศาสตร์และเทคโนโลยี" : config!.FacultyDeanTitle!;
            var trainingName = topic.Name.Trim();

            EnsureFontRegistered();
            QuestPDF.Settings.License = LicenseType.Community;

            return QuestPDF.Fluent.Document.Create(container =>
            {
                foreach (var name in names)
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4.Landscape());
                        page.Margin(0);
                        page.DefaultTextStyle(x => x.FontFamily("TH Sarabun New").FontSize(22).LineHeight(1.05f));

                        if (background != null)
                            page.Background().Image(background, ImageScaling.FitArea);

                        page.Content().PaddingHorizontal(60).PaddingTop(62).AlignTop().AlignCenter().Column(column =>
                        {
                            column.Spacing(2);

                            column.Item().AlignCenter().Row(row =>
                            {
                                row.Spacing(16);
                                row.AutoItem().Width(80).Height(80).AlignCenter().AlignMiddle().Padding(2).Element(e =>
                                {
                                    if (uniLogo != null) e.Image(uniLogo, ImageScaling.FitArea); else e.Text(" ");
                                });
                                row.AutoItem().Width(80).Height(80).AlignCenter().AlignMiddle().Padding(2).Element(e =>
                                {
                                    if (instLogo != null) e.Image(instLogo, ImageScaling.FitArea); else e.Text(" ");
                                });
                            });

                            column.Item().Height(5);
                            column.Item().AlignCenter().Text("เกียรติบัตรฉบับนี้ให้ไว้เพื่อแสดงว่า").FontSize(30).SemiBold().LineHeight(1.0f);
                            column.Item().Height(5);
                            column.Item().AlignCenter().Text(name).FontSize(30).Bold().FontColor("#1D4ED8").LineHeight(1.2f);
                            column.Item().AlignCenter().Text("ได้ผ่านการอบรม").FontSize(24).SemiBold().LineHeight(1.1f);
                            column.Item().AlignCenter().Text(trainingName).FontSize(26).Bold().FontColor("#B91C1C").LineHeight(1.1f);

                            column.Item().Height(5);
                            column.Item().AlignCenter().Text($"งานสัปดาห์วิทยาศาสตร์แห่งชาติ (ส่วนภูมิภาค) ประจำปี {year.YearNumber}").FontSize(26).SemiBold().LineHeight(1.5f);
                            if (!string.IsNullOrWhiteSpace(year.DateName))
                                column.Item().AlignCenter().Text($"ระหว่างวันที่ {year.DateName} {year.YearNumber} ณ มหาวิทยาลัยราชภัฏกาญจนบุรี").FontSize(26).SemiBold().LineHeight(1.0f);
                            column.Item().AlignCenter().Text("ขออวยพรให้ประสบความสุข ความเจริญก้าวหน้าตลอดไป").FontSize(24).SemiBold().LineHeight(1.5f);
                            column.Item().Height(20);

                            column.Item().AlignCenter().Width(700).PaddingHorizontal(12).Table(table =>
                            {
                                table.ColumnsDefinition(columns => { columns.RelativeColumn(1); columns.RelativeColumn(1); });
                                table.Cell().Row(1).Column(1).AlignCenter().Width(100).Height(36).AlignBottom().Element(e => { if (leftSignature != null) e.Image(leftSignature, ImageScaling.FitArea); else e.Text(" "); });
                                table.Cell().Row(1).Column(2).AlignCenter().Width(100).Height(36).AlignBottom().Element(e => { if (rightSignature != null) e.Image(rightSignature, ImageScaling.FitArea); else e.Text(" "); });
                                table.Cell().Row(2).Column(1).AlignCenter().Text(leftName).FontSize(18).Bold().LineHeight(1.0f);
                                table.Cell().Row(2).Column(2).AlignCenter().Text(rightName).FontSize(18).Bold().LineHeight(1.0f);
                                table.Cell().Row(3).Column(1).AlignCenter().Text(leftRole).FontSize(17).LineHeight(1.0f);
                                table.Cell().Row(3).Column(2).AlignCenter().Text(rightRole).FontSize(17).LineHeight(1.0f);
                            });
                        });
                    });
                }
            }).GeneratePdf();
        }

        // รูปใน wwwroot (หรือ wwwroot/uploads) / URL
        private async Task<byte[]?> LoadWebImageAsync(string? pathOrUrl)
        {
            if (string.IsNullOrWhiteSpace(pathOrUrl)) return null;
            if (pathOrUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                try { using var http = new HttpClient(); return await http.GetByteArrayAsync(pathOrUrl); }
                catch { return null; }
            }

            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var normalized = pathOrUrl.TrimStart('~').TrimStart('/');
            var full = Path.Combine(webRoot, normalized);
            if (File.Exists(full)) return await File.ReadAllBytesAsync(full);
            var uploads = Path.Combine(webRoot, "uploads", normalized);
            return File.Exists(uploads) ? await File.ReadAllBytesAsync(uploads) : null;
        }

        // ใบลงชื่อผู้เข้าอบรม: หัวเป็นชื่อหลักสูตร ตาราง ลำดับ / ชื่อ-นามสกุล / ลายมือชื่อ (เฉพาะคนที่ยังไม่ยกเลิก)
        public async Task<byte[]> GenerateTrainingSignInSheetAsync(int trainingtopicId)
        {
            var topic = await _context.Trainingtopics.AsNoTracking().FirstOrDefaultAsync(t => t.Id == trainingtopicId)
                ?? throw new KeyNotFoundException("ไม่พบหลักสูตรอบรม");

            var names = await LoadSignInNamesAsync(new[] { trainingtopicId });
            if (!names.TryGetValue(trainingtopicId, out var people))
                throw new KeyNotFoundException("ยังไม่มีผู้ลงทะเบียนในหลักสูตรนี้");

            return BuildSignInPdf(new[] { (topic.Name, people) });
        }

        // ใบลงชื่อทุกหลักสูตรของปี ไฟล์เดียว แต่ละหลักสูตรขึ้นหน้าใหม่ (ข้ามหลักสูตรที่ไม่มีผู้ลงทะเบียน)
        public async Task<byte[]> GenerateTrainingSignInSheetsByYearAsync(int yearId)
        {
            var topics = await _context.Trainingtopics.AsNoTracking()
                .Where(t => t.YearID == yearId)
                .OrderBy(t => t.TrainingDate).ThenBy(t => t.Name)
                .Select(t => new { t.Id, t.Name })
                .ToListAsync();

            var names = await LoadSignInNamesAsync(topics.Select(t => t.Id).ToList());
            var sheets = topics
                .Where(t => names.ContainsKey(t.Id))
                .Select(t => (t.Name, names[t.Id]))
                .ToList();

            if (sheets.Count == 0)
                throw new KeyNotFoundException("ยังไม่มีผู้ลงทะเบียนในหลักสูตรของปีนี้");

            return BuildSignInPdf(sheets);
        }

        private async Task<Dictionary<int, List<string>>> LoadSignInNamesAsync(ICollection<int> topicIds)
        {
            var rows = await _context.TrainingRegistrations.AsNoTracking()
                .Where(r => topicIds.Contains(r.TrainingtopicId) && r.Status != "ยกเลิกแล้ว")
                .OrderBy(r => r.RegisteredAt)
                .Select(r => new { r.TrainingtopicId, r.Prefix, r.FirstName, r.LastName })
                .ToListAsync();

            return rows
                .GroupBy(r => r.TrainingtopicId)
                .ToDictionary(g => g.Key, g => g.Select(p => $"{p.Prefix}{p.FirstName} {p.LastName}".Trim()).ToList());
        }

        private byte[] BuildSignInPdf(IEnumerable<(string Title, List<string> People)> sheets)
        {
            EnsureFontRegistered();
            QuestPDF.Settings.License = LicenseType.Community;

            return QuestPDF.Fluent.Document.Create(container =>
            {
                // หนึ่งหลักสูตร = หนึ่ง page set → ขึ้นหน้าใหม่เสมอ เลขหน้านับแยกต่อหลักสูตร
                foreach (var (title, people) in sheets)
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(30);
                        page.DefaultTextStyle(x => x.FontSize(16).FontFamily("TH Sarabun New"));

                        page.Header().PaddingBottom(10).AlignCenter().Text(title).FontSize(20).Bold();

                        page.Content().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(50);  // ลำดับ
                                columns.RelativeColumn(3);   // ชื่อ-นามสกุล
                                columns.RelativeColumn(2);   // ลายมือชื่อ
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(Head).Text("ลำดับ").Bold();
                                header.Cell().Element(Head).Text("ชื่อ-นามสกุล").Bold();
                                header.Cell().Element(Head).Text("ลายมือชื่อ").Bold();
                            });

                            var no = 1;
                            foreach (var name in people)
                            {
                                table.Cell().Element(Cell).AlignCenter().Text((no++).ToString());
                                table.Cell().Element(Cell).Text(name);
                                table.Cell().Element(Cell).Text("");
                            }
                        });

                        page.Footer().AlignRight().Text(x =>
                        {
                            x.Span("หน้า ");
                            x.CurrentPageNumber();
                            x.Span(" / ");
                            x.TotalPages();
                        });
                    });
                }
            }).GeneratePdf();

            static IContainer Head(IContainer c) =>
                c.Border(1).Background("#F1F5F9").PaddingVertical(6).AlignCenter().AlignMiddle();

            // แถวสูงพอให้เซ็นชื่อได้
            static IContainer Cell(IContainer c) =>
                c.Border(1).MinHeight(34).PaddingHorizontal(6).AlignMiddle();
        }

        public async Task<byte[]> GenerateTeamReportTableAsync(int competitionLevelID)
        {
            var query = _context.Teams
                .Where(t => t.CompetitionLevelID == competitionLevelID);

            query = query.Where(t => t.Status == SD.TeamStatus.CheckedIn || t.Status == SD.TeamStatus.Verified);

            var teams = await query
                .Include(t => t.Students)
                .Include(t => t.CompetitionLevel).ThenInclude(cl => cl.CompetitionYear).ThenInclude(cy => cy.Competition)
                .Include(t => t.CompetitionLevel).ThenInclude(cl => cl.CompetitionYear).ThenInclude(cy => cy.Year).ThenInclude(y => y.CertificateConfig)
                .Include(t => t.CompetitionLevel).ThenInclude(cl => cl.EducationLevel)
                .Include(t => t.schools)
                .ToListAsync();

            if (!teams.Any())
                throw new KeyNotFoundException("ไม่พบทีมในระดับการแข่งขันนี้");

            bool includeProjectColumn = teams.Any(t => !string.IsNullOrWhiteSpace(t.ProjectName));

            EnsureFontRegistered();

            QuestPDF.Settings.License = LicenseType.Community;

            var stream = new MemoryStream();

            QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);         // แนวตั้ง
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("TH Sarabun New"));

                    var levelName = teams.First().CompetitionLevel?.EducationLevel?.LevelName ?? "";
                    var competitionName = teams.First().CompetitionLevel?.CompetitionYear?.Competition?.CompetitionName ?? "";

                    page.Header().Element(e =>
                    {
                        e.PaddingVertical(15)
                         .AlignCenter()
                         .Text($"{competitionName} - {levelName}")
                         .FontSize(16)       // ใหญ่ขึ้น
                         .SemiBold();
                    });

                    page.Content().Table(table =>
                    {
                        // คอลัมน์ (เหมาะกับแนวตั้ง)
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1); // ลำดับ
                            columns.RelativeColumn(3); // โรงเรียน
                            columns.RelativeColumn(3); // ผู้สมัครแข่งขัน
                            columns.RelativeColumn(3); // ครูผู้ควบคุม
                            // ชื่อโครงงานอยู่แถวของตัวเองใต้ทีม (กว้างเต็ม 3 คอลัมน์) ไม่บีบคอลัมน์อื่น
                            //columns.RelativeColumn(1); // หมายเหตุ
                        });

                        // Header ของตาราง: ชัดขึ้น + กึ่งกลาง
                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCellStyle).Text("ลำดับ").SemiBold().AlignCenter();
                            header.Cell().Element(HeaderCellStyle).Text("โรงเรียน").SemiBold().AlignCenter();
                            header.Cell().Element(HeaderCellStyle).Text("ผู้สมัครแข่งขัน").SemiBold().AlignCenter();
                            header.Cell().Element(HeaderCellStyle).Text("ครูผู้ควบคุม").SemiBold().AlignCenter();
                            //header.Cell().Element(HeaderCellStyle).Text("หมายเหตุ").SemiBold().AlignCenter();
                        });


                        int index = 1;
                        foreach (var team in teams)
                        {
                            var teamName = string.IsNullOrWhiteSpace(team.schools.school_name)
                                ? "—"
                                : Regex.Replace(team.schools.school_name, @"\s*[\d๐-๙]+$", "");

                            var studentList = string.Join("\n",
                                team.Students
                                  .Where(s => HasExportableStudentName(s.FirstName, s.LastName))
                                  .Select((s, i) =>
                                  $"{i + 1}. {PrefixToString(s.TitlePrefix)} {s.FirstName} {s.LastName}"
                                  ));

                            // ✅ สร้างรายชื่อครูผู้ควบคุมจากฟิลด์แยก
                            var advisors = new List<string?>();
                            advisors.Add(FormatPerson(team.PrimaryTitlePrefix, team.PrimaryFirstName, team.PrimaryLastName));
                            advisors.Add(FormatPerson(team.SecondaryTitlePrefix, team.SecondaryFirstName, team.SecondaryLastName));

                            var advisorList = string.Join("\n",
                                advisors.Where(x => !string.IsNullOrWhiteSpace(x))
                                        .Select((t, i) => $"{i + 1}. {t}"));

                            // ==== แถวข้อมูล ====
                            var project = includeProjectColumn ? team.ProjectName?.Trim() : null;
                            var hasProject = !string.IsNullOrEmpty(project);
                            table.Cell().RowSpan(hasProject ? 2u : 1u).Element(CellStyle).AlignCenter().Text((index++).ToString()); // ลำดับ
                            table.Cell().Element(CellStyle).Text(teamName);             // โรงเรียน
                            table.Cell().Element(CellStyle).Text(studentList);          // ผู้สมัครแข่งขัน
                            table.Cell().Element(CellStyle).Text(string.IsNullOrWhiteSpace(advisorList) ? "—" : advisorList); // ครูผู้ควบคุม
                            if (hasProject)
                            {
                                // แถวชื่อโครงงาน: กว้างเต็ม (โรงเรียน → ครูผู้ควบคุม)
                                table.Cell().ColumnSpan(3).Element(ProjectCellStyle).Text(t =>
                                {
                                    t.Span("ชื่อโครงงาน: ").SemiBold();
                                    t.Span(project);
                                });
                            }
                                                                                                                              // table.Cell().Element(CellStyle).Text("");                 // หมายเหตุ (ถ้าจะใช้)
                        }
                    });

                    page.Footer().AlignRight().Text(x =>
                    {
                        x.Span("หน้าที่ ");
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                });
            }).GeneratePdf(stream);

            return stream.ToArray();

            // ==== Styles ====
            IContainer CellStyle(IContainer c) =>
                c.Border(1).Padding(5).AlignTop().AlignLeft();

            IContainer HeaderCellStyle(IContainer c) =>
                c.Border(1).Padding(6);

            IContainer ProjectCellStyle(IContainer c) =>
                c.Border(1).Background("#F3F4F6").PaddingVertical(4).PaddingHorizontal(5).AlignLeft();
        }
        public async Task<byte[]> GenerateTeamReportWordTeamAsync(int competitionLevelID)
        {
            var query = _context.Teams
                .Where(t => t.CompetitionLevelID == competitionLevelID);

            query = query.Where(t => t.Status == SD.TeamStatus.CheckedIn || t.Status == SD.TeamStatus.Verified);

            var teams = await query
                .Include(t => t.Students)
                .Include(t => t.CompetitionLevel).ThenInclude(cl => cl.CompetitionYear).ThenInclude(cy => cy.Competition)
                .Include(t => t.CompetitionLevel).ThenInclude(cl => cl.CompetitionYear).ThenInclude(cy => cy.Year)
                .Include(t => t.CompetitionLevel).ThenInclude(cl => cl.EducationLevel)
                .Include(t => t.schools)
                .ToListAsync();

            if (!teams.Any())
                throw new KeyNotFoundException("ไม่พบทีมในระดับการแข่งขันนี้");

            // แสดงคอลัมน์ “ชื่อโครงงาน” ก็ต่อเมื่อมีทีมที่ระบุจริง
            bool includeProjectColumn = teams.Any(t => !string.IsNullOrWhiteSpace(t.ProjectName));

            using var stream = new MemoryStream();
            using (var wordDoc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
            {
                var mainPart = wordDoc.AddMainDocumentPart();
                mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document();
                var body = new Body();

                // ===== หัวเรื่อง =====
                var levelName = teams.First().CompetitionLevel?.EducationLevel?.LevelName ?? "";
                var competitionName = teams.First().CompetitionLevel?.CompetitionYear?.Competition?.CompetitionName ?? "";

                // ย่อหน้า: จัดกลาง + ระยะห่างบน/ล่าง + ขนาดฟอนต์ใหญ่
                var headingPara = new Paragraph(
                    new ParagraphProperties(
                        new Justification { Val = JustificationValues.Center },
                        new SpacingBetweenLines { Before = "200", After = "200" }   // 10pt ก่อน-หลัง โดยประมาณ
                    ),
                   new Run(
                      CreateFontRunProperties(40, bold: true),
                       new Text($"{competitionName} - {levelName}")
                    )
                );
                body.Append(headingPara);

                // ===== ตาราง =====
                var table = new Table();

                // เส้นตาราง + ให้กว้างเต็มหน้า
                var tblProps = new TableProperties(
                    new TableWidth { Type = TableWidthUnitValues.Auto },
                    new TableBorders(
                        new TopBorder { Val = BorderValues.Single, Size = 6 },
                        new BottomBorder { Val = BorderValues.Single, Size = 6 },
                        new LeftBorder { Val = BorderValues.Single, Size = 6 },
                        new RightBorder { Val = BorderValues.Single, Size = 6 },
                        new InsideHorizontalBorder { Val = BorderValues.Single, Size = 6 },
                        new InsideVerticalBorder { Val = BorderValues.Single, Size = 6 }
                    )
                );
                table.AppendChild(tblProps);
                table.AppendChild(CreateTableGrid("800", "2000", "3000", "3000", "1200"));

                // ----- แถวหัวตาราง -----
                var headerRow = new TableRow();

                headerRow.Append(
                    CreateHeaderCell("ลำดับ", "800"),
                    CreateHeaderCell("โรงเรียน", "2000")
                );

                headerRow.Append(
                    CreateHeaderCell("ผู้สมัครแข่งขัน", "3000"),
                    CreateHeaderCell("ครูผู้ควบคุม", "3000"),
                    CreateHeaderCell("หมายเหตุ", "1200")
                );

                table.Append(headerRow);

                // ----- แถวข้อมูล -----
                int index = 1;
                foreach (var team in teams)
                {

                    var schoolNameRaw = string.IsNullOrWhiteSpace(team.schools.school_name) ? "—" : team.schools.school_name;
                    // The trailing 3 is part of this school's official branch name.
                    var schoolName = schoolNameRaw == "โรงเรียนบรรหารแจ่มใสวิทยา 3"
                        ? schoolNameRaw
                        : Regex.Replace(schoolNameRaw, @"\s*[\d๐-๙]+$", "");

                    var studentList = team.Students
                        .Where(s => HasExportableStudentName(s.FirstName, s.LastName))
                        .Select((s, i) => $"{i + 1}. {PrefixToString(s.TitlePrefix)} {s.FirstName} {s.LastName}");
                    var advisorLines = new List<string?>();
                    advisorLines.Add(FormatPerson(team.PrimaryTitlePrefix, team.PrimaryFirstName, team.PrimaryLastName));
                    advisorLines.Add(FormatPerson(team.SecondaryTitlePrefix, team.SecondaryFirstName, team.SecondaryLastName));


                    var project = includeProjectColumn ? team.ProjectName?.Trim() : null;
                    var hasProject = !string.IsNullOrEmpty(project);

                    var row = new TableRow();
                    row.Append(
                        MergeDown(CreateTableCell(index++.ToString(), "800"), hasProject, restart: true),
                        CreateTableCell(schoolName, "2000")
                    );

                    row.Append(
                        CreateParagraphCell(studentList, "3000"),
                         CreateParagraphCell(advisorLines!.Where(x => !string.IsNullOrWhiteSpace(x))!, "3000"),
                        CreateTableCell("", "1200")
                    );

                    table.Append(row);
                    if (hasProject)
                        table.Append(CreateProjectRow(project!, "800", "9200", 4));
                }

                body.Append(table);
                mainPart.Document.Append(body);
            }

            return stream.ToArray();
        }
        public async Task<byte[]> GenerateWinnersFormAsync(int competitionLevelID, CancellationToken ct = default)
        {
            var registrationMode = await _context.CompetitionLevels
                .Where(level => level.CompetitionLevelID == competitionLevelID)
                .Select(level => level.CompetitionYear!.RegistrationMode)
                .FirstOrDefaultAsync(ct);

            List<Team> teams;
            if (registrationMode == "individual")
            {
                // ผู้สมัครเดี่ยวถูกเก็บเป็น Student ที่ผูกกับทีม จัดรูปเป็นทีม
                // ชั่วคราวทีมละหนึ่งคน เพื่อใช้รูปแบบ PDF เดียวกับผลรางวัลแบบทีม
                var students = await _context.Students
                    .Where(student =>
                        student.Team!.CompetitionLevelID == competitionLevelID &&
                        student.RewardTypeID != null &&
                        (student.IsCheckedIn || student.Team.Status == SD.TeamStatus.CheckedIn))
                    .Include(student => student.RewardType)
                    .Include(student => student.Team).ThenInclude(team => team.schools)
                    .Include(student => student.Team).ThenInclude(team => team.CompetitionLevel)
                        .ThenInclude(level => level!.CompetitionYear).ThenInclude(year => year!.Competition)
                    .Include(student => student.Team).ThenInclude(team => team.CompetitionLevel)
                        .ThenInclude(level => level!.CompetitionYear).ThenInclude(year => year!.Year)
                    .AsNoTracking()
                    .ToListAsync(ct);

                teams = students.Select(student => new Team
                {
                    RewardType = student.RewardType!,
                    Students = new List<Student> { student },
                    schools = student.Team!.schools,
                    CompetitionLevel = student.Team.CompetitionLevel,
                }).ToList();
            }
            else
            {
                // 1) ดึงทีมที่มี RewardTypeID (ถือว่า "ได้รางวัล")
                teams = await _context.Teams
                    .Where(t => t.CompetitionLevelID == competitionLevelID && t.RewardTypeID != null)
                    .Include(t => t.Students)
                    .Include(t => t.schools)
                    .Include(t => t.RewardType)
                    .Include(t => t.CompetitionLevel).ThenInclude(cl => cl.CompetitionYear).ThenInclude(cy => cy.Competition)
                    .Include(t => t.CompetitionLevel).ThenInclude(cl => cl.CompetitionYear).ThenInclude(cy => cy.Year)
                    .AsNoTracking()
                    .ToListAsync(ct);
            }

            if (!teams.Any())
                throw new KeyNotFoundException("ไม่พบทีมผู้ชนะในระดับนี้");

            // 2) แปลงเป็น items + จัดหมวดแบบยืดหยุ่น
            var items = teams.Select(t =>
            {
                var rewardRaw = t.RewardType?.RewardTypeName ?? "";
                var cls = ClassifyReward(rewardRaw); // (order, main, rank, isUnknown)
                return new
                {
                    cls.order,
                    cls.main,
                    cls.rank,
                    cls.isUnknown,
                    rewardRaw, // เก็บไว้เผื่อโชว์ตรง ๆ ถ้า unknown
                    School = t.schools?.school_name ?? "",
                    Students = (t.Students ?? new List<Student>())
                               .Where(s => s.IsMainMember == true)
                               .OrderBy(s => s.StudentID)
                               .ToList()
                };
            })
            // **สำคัญ**: ไม่ทิ้ง unknown (เพื่อไม่ให้กลายเป็นตารางว่าง)
            // ถ้าต้องการตัด "เข้าร่วม" ออกไปเลย ทำใน ClassifyReward แล้วให้ order=99,isUnknown=true ก็ได้
            .Where(x => x.order < 99) // 99 = ทิ้งเฉพาะพวก "เข้าร่วม"
            .OrderBy(x => x.order)
            .ToList();

            if (!items.Any())
                throw new KeyNotFoundException("ไม่พบทีมผู้ชนะในระดับนี้ (ตรวจสอบชื่อรางวัล/เว้นวรรค)");

            // 3) จัดกลุ่ม: use main+rank เป็นคีย์ ถ้า unknown ให้ใช้ rewardRaw เป็น main เพื่อให้มองเห็นค่าจริง
            var groups = items
                .Select(x => new
                {
                    KeyMain = x.isUnknown ? NormalizeDisplay(x.rewardRaw) : x.main,
                    KeyRank = x.isUnknown ? "" : x.rank,
                    x.School,
                    x.Students,
                    x.order
                })
                .GroupBy(x => (x.KeyMain, x.KeyRank))
                .OrderBy(g => g.Min(i => i.order))
                .ToList();

            // 4) ข้อมูลหัวกระดาษ
            var any = teams.First();
            var th = new System.Globalization.CultureInfo("th-TH");
            var datetime = any.CompetitionLevel?.CompetitionDateTime.ToString("dd MMMM yyyy", th) ?? "";
            var title = any.CompetitionLevel?.CompetitionYear?.Competition?.CompetitionName ?? "";
            var year = any.CompetitionLevel?.CompetitionYear?.Year?.YearNumber ?? 0;

            // 5) ฟอนต์ + สร้าง PDF
            FontManager.RegisterFont(File.OpenRead("Fonts/THSarabunNew.ttf"));
            QuestPDF.Settings.License = LicenseType.Community;

            var stream = new MemoryStream();
            QuestPDF.Fluent.Document.Create(doc =>
            {
                doc.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontFamily("TH Sarabun New").FontSize(16));

                    // Header
                    page.Header().Column(col =>
                    {
                        col.Item().AlignCenter().Text($"ผล{title}").FontSize(18).Bold();
                        // TODO: ถ้าต้องใส่ระดับชั้นแบบไดนามิก ให้ดึงจาก CompetitionLevel
                        col.Item().AlignCenter().Text($"งานสัปดาห์วิทยาศาสตร์แห่งชาติ (ส่วนภูมิภาค) ประจำปี {year}").FontSize(16);
                        col.Item().AlignCenter().Text("มหาวิทยาลัยราชภัฏกาญจนบุรี").FontSize(16);
                        col.Item().AlignCenter().Text(datetime).FontSize(16);
                        col.Item().AlignCenter().Text(new string('*', 90)).FontSize(14);
                    });

                    // Content
                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(3); // รางวัล
                            c.RelativeColumn(6); // ชื่อ-สกุล
                            c.RelativeColumn(6); // โรงเรียน
                        });

                        table.Header(h =>
                        {
                            h.Cell().Element(HeaderCell).Text("รางวัล").AlignCenter().SemiBold();
                            h.Cell().Element(HeaderCell).Text("ชื่อ-นามสกุล").AlignCenter().SemiBold();
                            h.Cell().Element(HeaderCell).Text("โรงเรียน").AlignCenter().SemiBold();
                        });

                        foreach (var g in groups)
                        {
                            bool first = true;
                            foreach (var row in g)
                            {
                                // คอลัมน์ "รางวัล" — แสดงเฉพาะบรรทัดแรกของแต่ละกลุ่ม
                                table.Cell().Element(c =>
                                {
                                    c.Border(1).Padding(6).AlignCenter().AlignMiddle().Column(col =>
                                    {
                                        if (first)
                                        {
                                            col.Item().AlignCenter().Text(g.Key.KeyMain);
                                            if (!string.IsNullOrWhiteSpace(g.Key.KeyRank))
                                                col.Item().AlignCenter().Text(g.Key.KeyRank);
                                        }
                                        else
                                        {
                                            col.Item().Text("");
                                        }
                                    });
                                });
                                first = false;

                                // คอลัมน์ "ชื่อ-สกุล" (สมาชิกหลัก)
                                var names = row.Students != null && row.Students.Any(s => HasExportableStudentName(s.FirstName, s.LastName))
                                    ? string.Join("\n", row.Students
                                        .Where(s => HasExportableStudentName(s.FirstName, s.LastName))
                                        .Select(s => $" {PrefixToString(s.TitlePrefix)} {s.FirstName} {s.LastName}"))
                                    : "-";
                                table.Cell().Element(BodyCell).Text(names).FontSize(12);

                                // คอลัมน์ "โรงเรียน"
                                table.Cell().Element(BodyCell).Text(row.School ?? "-").FontSize(13);
                            }
                        }
                    });

                    // Footer
                    page.Footer().Column(col =>
                    {
                        col.Item()
                           .PaddingTop(30) // ห่างจากด้านบนมากขึ้น
                           .AlignRight()
                           .Text("ลายเซ็นชื่อคณะกรรมการ")
                           .FontSize(16);

                        var labels = new[]
                        {
                        "ประธานกรรมการ",
                        "กรรมการ",
                        "กรรมการ",
                        "กรรมการ",
                        "กรรมการและเลขานุการ"
                    };

                        foreach (var label in labels)
                        {
                            col.Item()
                               .PaddingTop(4)      // เพิ่มช่องว่างก่อนเส้นเซ็น
                               .PaddingBottom(10)  // เพิ่มระยะห่างแต่ละคน
                               .AlignRight()
                               .Column(inner =>
                               {
                                   inner.Item()
                                        .Text("ลงชื่อ ................................................................")
                                        .FontSize(14);

                                   inner.Item()
                                        .PaddingTop(2)
                                        .Text(label)
                                        .FontSize(14)
                                        .AlignRight();
                               });
                        }
                    });
                });
            }).GeneratePdf(stream);

            // --------- (แนะนำ) Log รายการรางวัลที่อ่านเจอ เพื่อดีบักบนเซิร์ฟเวอร์ ---------
            try
            {
                var rewardSeen = teams.Select(t => t.RewardType!.RewardTypeName?.Trim())
                                      .Where(s => !string.IsNullOrWhiteSpace(s))
                                      .Distinct()
                                      .ToList();
                Console.WriteLine("[Winners Export] Rewards in DB: " + string.Join(" | ", rewardSeen));
            }
            catch { /* ignore */ }

            return stream.ToArray();


            // ================= Helpers =================

            static string ThaiToArabicDigits(string s) => s
                .Replace("๐", "0").Replace("๑", "1").Replace("๒", "2").Replace("๓", "3").Replace("๔", "4")
                .Replace("๕", "5").Replace("๖", "6").Replace("๗", "7").Replace("๘", "8").Replace("๙", "9");

            // ล้างช่องว่างทุกชนิด + เคสเเข/แข + ปรับ "เข้าร่วมการเเข่งขัน" -> "เข้าร่วมการแข่งขัน"
            static string NormalizeCompact(string? raw)
            {
                var s = (raw ?? "").Trim();
                s = ThaiToArabicDigits(s);
                s = s.Replace("เแข", "แข่งขัน").Replace("เเข่งขัน", "แข่งขัน")
                     .Replace("เข้าร่วมการเเข่งขัน", "เข้าร่วมการแข่งขัน");
                s = new string(s.Where(ch => !char.IsWhiteSpace(ch)).ToArray());
                return s;
            }

            // ใช้สำหรับโชว์ค่าดิบใน PDF เมื่อไม่รู้จักหมวด (unknown) — คงเว้นวรรคให้อ่านง่าย
            static string NormalizeDisplay(string? raw)
            {
                var s = (raw ?? "").Trim();
                s = s.Replace("เแข", "แข่งขัน").Replace("เเข่งขัน", "แข่งขัน")
                     .Replace("เข้าร่วมการเเข่งขัน", "เข้าร่วมการแข่งขัน");
                return s;
            }

            // จัดหมวดรางวัล -> (ลำดับ, ชื่อหลัก, อันดับ, isUnknown)
            static (int order, string main, string rank, bool isUnknown) ClassifyReward(string? raw)
            {
                var compact = NormalizeCompact(raw);
                if (string.IsNullOrEmpty(compact)) return (99, "", "", true);

                // ทิ้ง "เข้าร่วม"
                if (compact.Contains("เข้าร่วม")) return (99, "", "", true);

                // รองชนะเลิศ (ปกติ)
                if (compact.Contains("รองชนะเลิศ"))
                {
                    if (compact.Contains("อันดับ1") || compact.Contains("อันดับที่1") || compact.Contains("อันดับหนึ่ง"))
                        return (20, "รางวัลรองชนะเลิศ", "อันดับ 1", false);
                    if (compact.Contains("อันดับ2") || compact.Contains("อันดับที่2") || compact.Contains("อันดับสอง"))
                        return (30, "รางวัลรองชนะเลิศ", "อันดับ 2", false);
                    return (25, "รางวัลรองชนะเลิศ", "", false);
                }

                // เคสสะกด "ชนะเลิศอันดับ1/2" แต่จริง ๆ หมายถึง "รองชนะเลิศ"
                if (compact.Contains("ชนะเลิศ") && compact.Contains("อันดับ") && !compact.Contains("รอง"))
                {
                    if (compact.Contains("อันดับ1") || compact.Contains("อันดับที่1") || compact.Contains("อันดับหนึ่ง"))
                        return (20, "รางวัลรองชนะเลิศ", "อันดับ 1", false);
                    if (compact.Contains("อันดับ2") || compact.Contains("อันดับที่2") || compact.Contains("อันดับสอง"))
                        return (30, "รางวัลรองชนะเลิศ", "อันดับ 2", false);
                }

                // ชนะเลิศ (ไม่มี "รอง"/"อันดับ")
                if (compact.Contains("ชนะเลิศ") && !compact.Contains("รอง") && !compact.Contains("อันดับ"))
                    return (10, "รางวัลชนะเลิศ", "", false);

                // ชมเชย
                if (compact.Contains("ชมเชย"))
                    return (40, "รางวัลชมเชย", "", false);

                // ไม่รู้จัก -> ไม่ทิ้ง ให้ถือเป็น unknown (จะโชว์ชื่อรางวัลดิบ)
                return (50, NormalizeDisplay(raw), "", true);
            }

            static IContainer HeaderCell(IContainer c) => c.Border(1).Padding(6).AlignCenter().AlignMiddle();
            static IContainer BodyCell(IContainer c) => c.Border(1).Padding(6).AlignTop().AlignLeft();

            static string PrefixToString(ProjectScientDay2568.Utility.SD.TitlePrefix prefix)
            {
                return prefix switch
                {
                    ProjectScientDay2568.Utility.SD.TitlePrefix.เด็กชาย => "เด็กชาย",
                    ProjectScientDay2568.Utility.SD.TitlePrefix.เด็กหญิง => "เด็กหญิง",
                    ProjectScientDay2568.Utility.SD.TitlePrefix.นาย => "นาย",
                    ProjectScientDay2568.Utility.SD.TitlePrefix.นางสาว => "นางสาว",
                    ProjectScientDay2568.Utility.SD.TitlePrefix.นาง => "นาง",
                    _ => ""
                };
            }
        }

        public async Task<byte[]> GenerateWinnersFormByYearAsync(int yearID, CancellationToken ct = default)
        {
            var levelIds = await _context.CompetitionLevels
                .Where(level => level.CompetitionYear!.YearID == yearID)
                .OrderBy(level => level.CompetitionYear!.Competition!.CompetitionName)
                .ThenBy(level => level.CompetitionDateTime)
                .Select(level => level.CompetitionLevelID)
                .ToListAsync(ct);

            if (levelIds.Count == 0)
                throw new KeyNotFoundException("ไม่พบกิจกรรมในปีการแข่งขันนี้");

            var pageData = new List<(List<Team> Teams, List<(int order, string main, string rank, bool isUnknown, string rewardRaw, string School, List<Student> Students)> Rows)>();

            foreach (var competitionLevelID in levelIds)
            {
                var registrationMode = await _context.CompetitionLevels
                    .Where(level => level.CompetitionLevelID == competitionLevelID)
                    .Select(level => level.CompetitionYear!.RegistrationMode)
                    .FirstOrDefaultAsync(ct);

                List<Team> teams;
                if (registrationMode == "individual")
                {
                    var students = await _context.Students
                        .Where(student =>
                            student.Team!.CompetitionLevelID == competitionLevelID &&
                            student.RewardTypeID != null &&
                            (student.IsCheckedIn || student.Team.Status == SD.TeamStatus.CheckedIn))
                        .Include(student => student.RewardType)
                        .Include(student => student.Team).ThenInclude(team => team.schools)
                        .Include(student => student.Team).ThenInclude(team => team.CompetitionLevel)
                            .ThenInclude(level => level!.CompetitionYear).ThenInclude(year => year!.Competition)
                        .Include(student => student.Team).ThenInclude(team => team.CompetitionLevel)
                            .ThenInclude(level => level!.CompetitionYear).ThenInclude(year => year!.Year)
                        .Include(student => student.Team).ThenInclude(team => team.CompetitionLevel)
                            .ThenInclude(level => level!.EducationLevel)
                        .AsNoTracking()
                        .ToListAsync(ct);

                    teams = students.Select(student => new Team
                    {
                        RewardType = student.RewardType!,
                        Students = new List<Student> { student },
                        schools = student.Team!.schools,
                        CompetitionLevel = student.Team.CompetitionLevel,
                    }).ToList();
                }
                else
                {
                    teams = await _context.Teams
                        .Where(team => team.CompetitionLevelID == competitionLevelID && team.RewardTypeID != null)
                        .Include(team => team.Students)
                        .Include(team => team.schools)
                        .Include(team => team.RewardType)
                        .Include(team => team.CompetitionLevel).ThenInclude(level => level!.CompetitionYear).ThenInclude(year => year!.Competition)
                        .Include(team => team.CompetitionLevel).ThenInclude(level => level!.CompetitionYear).ThenInclude(year => year!.Year)
                        .Include(team => team.CompetitionLevel).ThenInclude(level => level!.EducationLevel)
                        .AsNoTracking()
                        .ToListAsync(ct);
                }

                if (teams.Count == 0)
                    continue;

                var rows = teams.Select(team =>
                {
                    var rewardRaw = team.RewardType?.RewardTypeName ?? "";
                    var classification = ClassifyReward(rewardRaw);
                    return (
                        classification.order,
                        classification.main,
                        classification.rank,
                        classification.isUnknown,
                        rewardRaw,
                        School: team.schools?.school_name ?? "",
                        Students: (team.Students ?? new List<Student>())
                            .Where(student => student.IsMainMember == true)
                            .OrderBy(student => student.StudentID)
                            .ToList());
                })
                .Where(row => row.order < 99)
                .OrderBy(row => row.order)
                .ToList();

                if (rows.Count > 0)
                    pageData.Add((teams, rows));
            }

            if (pageData.Count == 0)
                throw new KeyNotFoundException("ไม่พบทีมผู้ชนะในปีการแข่งขันนี้");

            FontManager.RegisterFont(File.OpenRead("Fonts/THSarabunNew.ttf"));
            QuestPDF.Settings.License = LicenseType.Community;

            using var stream = new MemoryStream();
            QuestPDF.Fluent.Document.Create(doc =>
            {
                foreach (var report in pageData)
                {
                    var groups = report.Rows
                        .Select(row => new
                        {
                            KeyMain = row.isUnknown ? NormalizeDisplay(row.rewardRaw) : row.main,
                            KeyRank = row.isUnknown ? "" : row.rank,
                            row.School,
                            row.Students,
                            row.order
                        })
                        .GroupBy(row => (row.KeyMain, row.KeyRank))
                        .OrderBy(group => group.Min(row => row.order))
                        .ToList();

                    var any = report.Teams.First();
                    var th = new System.Globalization.CultureInfo("th-TH");
                    var datetime = any.CompetitionLevel?.CompetitionDateTime.ToString("dd MMMM yyyy", th) ?? "";
                    var title = any.CompetitionLevel?.CompetitionYear?.Competition?.CompetitionName ?? "";
                    var levelName = any.CompetitionLevel?.EducationLevel?.LevelName ?? "";
                    var year = any.CompetitionLevel?.CompetitionYear?.Year?.YearNumber ?? 0;

                    doc.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(40);
                        page.DefaultTextStyle(style => style.FontFamily("TH Sarabun New").FontSize(16));

                        page.Header().Column(column =>
                        {
                            column.Item().AlignCenter().Text($"ผล{title} ระดับชั้น {levelName}").FontSize(18).Bold();
                            column.Item().AlignCenter().Text($"งานสัปดาห์วิทยาศาสตร์แห่งชาติ (ส่วนภูมิภาค) ประจำปี {year}").FontSize(16);
                            column.Item().AlignCenter().Text("มหาวิทยาลัยราชภัฏกาญจนบุรี").FontSize(16);
                            column.Item().AlignCenter().Text(datetime).FontSize(16);
                            column.Item().AlignCenter().Text(new string('*', 90)).FontSize(14);
                        });

                        page.Content().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(6);
                                columns.RelativeColumn(6);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderCell).Text("รางวัล").AlignCenter().SemiBold();
                                header.Cell().Element(HeaderCell).Text("ชื่อ-นามสกุล").AlignCenter().SemiBold();
                                header.Cell().Element(HeaderCell).Text("โรงเรียน").AlignCenter().SemiBold();
                            });

                            foreach (var group in groups)
                            {
                                var first = true;
                                foreach (var row in group)
                                {
                                    table.Cell().Element(cell =>
                                    {
                                        cell.Border(1).Padding(6).AlignCenter().AlignMiddle().Column(column =>
                                        {
                                            if (first)
                                            {
                                                column.Item().AlignCenter().Text(group.Key.KeyMain);
                                                if (!string.IsNullOrWhiteSpace(group.Key.KeyRank))
                                                    column.Item().AlignCenter().Text(group.Key.KeyRank);
                                            }
                                            else
                                            {
                                                column.Item().Text("");
                                            }
                                        });
                                    });
                                    first = false;

                                    var names = row.Students.Any(student => HasExportableStudentName(student.FirstName, student.LastName))
                                        ? string.Join("\n", row.Students
                                            .Where(student => HasExportableStudentName(student.FirstName, student.LastName))
                                            .Select(student => $" {PrefixToString(student.TitlePrefix)} {student.FirstName} {student.LastName}"))
                                        : "-";
                                    table.Cell().Element(BodyCell).Text(names).FontSize(12);
                                    table.Cell().Element(BodyCell).Text(row.School ?? "-").FontSize(13);
                                }
                            }
                        });

                        page.Footer().Column(column =>
                        {
                            column.Item().PaddingTop(30).AlignRight().Text("ลายเซ็นชื่อคณะกรรมการ").FontSize(16);
                            var labels = new[] { "ประธานกรรมการ", "กรรมการ", "กรรมการ", "กรรมการ", "กรรมการและเลขานุการ" };
                            foreach (var label in labels)
                            {
                                column.Item().PaddingTop(4).PaddingBottom(10).AlignRight().Column(inner =>
                                {
                                    inner.Item().Text("ลงชื่อ ................................................................").FontSize(14);
                                    inner.Item().PaddingTop(2).Text(label).FontSize(14).AlignRight();
                                });
                            }
                        });
                    });
                }
            }).GeneratePdf(stream);

            return stream.ToArray();

            static string ThaiToArabicDigits(string value) => value
                .Replace("๐", "0").Replace("๑", "1").Replace("๒", "2").Replace("๓", "3").Replace("๔", "4")
                .Replace("๕", "5").Replace("๖", "6").Replace("๗", "7").Replace("๘", "8").Replace("๙", "9");

            static string NormalizeCompact(string? raw)
            {
                var value = ThaiToArabicDigits((raw ?? "").Trim());
                value = value.Replace("เแข", "แข่งขัน").Replace("เเข่งขัน", "แข่งขัน")
                    .Replace("เข้าร่วมการเเข่งขัน", "เข้าร่วมการแข่งขัน");
                return new string(value.Where(character => !char.IsWhiteSpace(character)).ToArray());
            }

            static string NormalizeDisplay(string? raw) => (raw ?? "").Trim()
                .Replace("เแข", "แข่งขัน").Replace("เเข่งขัน", "แข่งขัน")
                .Replace("เข้าร่วมการเเข่งขัน", "เข้าร่วมการแข่งขัน");

            static (int order, string main, string rank, bool isUnknown) ClassifyReward(string? raw)
            {
                var compact = NormalizeCompact(raw);
                if (string.IsNullOrEmpty(compact) || compact.Contains("เข้าร่วม")) return (99, "", "", true);
                if (compact.Contains("รองชนะเลิศ"))
                {
                    if (compact.Contains("อันดับ1") || compact.Contains("อันดับที่1") || compact.Contains("อันดับหนึ่ง")) return (20, "รางวัลรองชนะเลิศ", "อันดับ 1", false);
                    if (compact.Contains("อันดับ2") || compact.Contains("อันดับที่2") || compact.Contains("อันดับสอง")) return (30, "รางวัลรองชนะเลิศ", "อันดับ 2", false);
                    return (25, "รางวัลรองชนะเลิศ", "", false);
                }
                if (compact.Contains("ชนะเลิศ") && compact.Contains("อันดับ") && !compact.Contains("รอง"))
                {
                    if (compact.Contains("อันดับ1") || compact.Contains("อันดับที่1") || compact.Contains("อันดับหนึ่ง")) return (20, "รางวัลรองชนะเลิศ", "อันดับ 1", false);
                    if (compact.Contains("อันดับ2") || compact.Contains("อันดับที่2") || compact.Contains("อันดับสอง")) return (30, "รางวัลรองชนะเลิศ", "อันดับ 2", false);
                }
                if (compact.Contains("ชนะเลิศ") && !compact.Contains("รอง") && !compact.Contains("อันดับ")) return (10, "รางวัลชนะเลิศ", "", false);
                if (compact.Contains("ชมเชย")) return (40, "รางวัลชมเชย", "", false);
                return (50, NormalizeDisplay(raw), "", true);
            }

            static IContainer HeaderCell(IContainer container) => container.Border(1).Padding(6).AlignCenter().AlignMiddle();
            static IContainer BodyCell(IContainer container) => container.Border(1).Padding(6).AlignTop().AlignLeft();
            static string PrefixToString(ProjectScientDay2568.Utility.SD.TitlePrefix prefix) => prefix switch
            {
                ProjectScientDay2568.Utility.SD.TitlePrefix.เด็กชาย => "เด็กชาย",
                ProjectScientDay2568.Utility.SD.TitlePrefix.เด็กหญิง => "เด็กหญิง",
                ProjectScientDay2568.Utility.SD.TitlePrefix.นาย => "นาย",
                ProjectScientDay2568.Utility.SD.TitlePrefix.นางสาว => "นางสาว",
                ProjectScientDay2568.Utility.SD.TitlePrefix.นาง => "นาง",
                _ => ""
            };
        }

        private sealed record WinnersWordRow(int Order, string Award, string School, List<string> Names);
        private sealed record WinnersWordSection(string CompetitionName, string LevelName, int YearNumber, string Date, List<WinnersWordRow> Rows);

        public Task<byte[]> GenerateWinnersFormWordAsync(int competitionLevelId, CancellationToken ct = default)
        {
            return GenerateWinnersFormWordForLevelsAsync(new[] { competitionLevelId }, ct);
        }

        public async Task<byte[]> GenerateWinnersFormByYearWordAsync(int yearId, CancellationToken ct = default)
        {
            var levelIds = await _context.CompetitionLevels
                .Where(level => level.CompetitionYear!.YearID == yearId)
                .Select(level => level.CompetitionLevelID)
                .ToListAsync(ct);

            if (levelIds.Count == 0)
                throw new KeyNotFoundException("ไม่พบกิจกรรมในปีการแข่งขันนี้");

            return await GenerateWinnersFormWordForLevelsAsync(levelIds, ct);
        }

        private async Task<byte[]> GenerateWinnersFormWordForLevelsAsync(IReadOnlyCollection<int> levelIds, CancellationToken ct)
        {
            var levels = await _context.CompetitionLevels
                .Where(level => levelIds.Contains(level.CompetitionLevelID))
                .Include(level => level.EducationLevel)
                .Include(level => level.CompetitionYear).ThenInclude(year => year!.Competition)
                .Include(level => level.CompetitionYear).ThenInclude(year => year!.Year)
                .AsNoTracking()
                .OrderBy(level => level.CompetitionYear!.Competition!.CompetitionName)
                .ThenBy(level => level.EducationLevel!.LevelName)
                .ToListAsync(ct);

            if (levels.Count == 0)
                throw new KeyNotFoundException("ไม่พบระดับการแข่งขันนี้");

            var individualLevelIds = levels
                .Where(level => level.CompetitionYear?.RegistrationMode == "individual")
                .Select(level => level.CompetitionLevelID)
                .ToList();
            var teamLevelIds = levels
                .Where(level => level.CompetitionYear?.RegistrationMode != "individual")
                .Select(level => level.CompetitionLevelID)
                .ToList();

            var teams = teamLevelIds.Count == 0
                ? new List<Team>()
                : await _context.Teams
                    .Where(team => team.CompetitionLevelID.HasValue &&
                        teamLevelIds.Contains(team.CompetitionLevelID.Value) && team.RewardTypeID != null)
                    .Include(team => team.Students)
                    .Include(team => team.schools)
                    .Include(team => team.RewardType)
                    .AsNoTracking()
                    .ToListAsync(ct);

            var individualStudents = individualLevelIds.Count == 0
                ? new List<Student>()
                : await _context.Students
                    .Where(student => student.Team!.CompetitionLevelID.HasValue &&
                        individualLevelIds.Contains(student.Team.CompetitionLevelID.Value) &&
                        student.RewardTypeID != null &&
                        (student.IsCheckedIn || student.Team.Status == SD.TeamStatus.CheckedIn))
                    .Include(student => student.RewardType)
                    .Include(student => student.Team).ThenInclude(team => team!.schools)
                    .AsNoTracking()
                    .ToListAsync(ct);

            var sections = new List<WinnersWordSection>();
            foreach (var level in levels)
            {
                var rows = new List<WinnersWordRow>();
                if (level.CompetitionYear?.RegistrationMode == "individual")
                {
                    foreach (var student in individualStudents.Where(student => student.Team?.CompetitionLevelID == level.CompetitionLevelID))
                    {
                        var award = ClassifyWinnersWordAward(student.RewardType?.RewardTypeName);
                        if (award.Order == 99) continue;

                        var name = HasExportableStudentName(student.FirstName, student.LastName)
                            ? FormatPerson(student.TitlePrefix, student.FirstName, student.LastName) ?? "-"
                            : "-";
                        rows.Add(new WinnersWordRow(
                            award.Order,
                            award.Label,
                            student.Team?.schools?.school_name ?? "",
                            new List<string> { name }));
                    }
                }
                else
                {
                    foreach (var team in teams.Where(team => team.CompetitionLevelID == level.CompetitionLevelID))
                    {
                        var award = ClassifyWinnersWordAward(team.RewardType?.RewardTypeName);
                        if (award.Order == 99) continue;

                        var names = team.Students?
                            .Where(student => student.IsMainMember && HasExportableStudentName(student.FirstName, student.LastName))
                            .OrderBy(student => student.StudentID)
                            .Select(student => FormatPerson(student.TitlePrefix, student.FirstName, student.LastName) ?? "-")
                            .ToList() ?? new List<string>();
                        if (names.Count == 0) names.Add("-");

                        rows.Add(new WinnersWordRow(
                            award.Order,
                            award.Label,
                            team.schools?.school_name ?? "",
                            names));
                    }
                }

                if (rows.Count == 0) continue;

                sections.Add(new WinnersWordSection(
                    level.CompetitionYear?.Competition?.CompetitionName ?? "",
                    level.EducationLevel?.LevelName ?? "",
                    level.CompetitionYear?.Year?.YearNumber ?? 0,
                    level.CompetitionDateTime.ToString("dd MMMM yyyy", CultureInfo.GetCultureInfo("th-TH")),
                    rows.OrderBy(row => row.Order).ToList()));
            }

            if (sections.Count == 0)
                throw new KeyNotFoundException("ไม่พบผู้ชนะในระดับหรือปีการแข่งขันนี้");

            using var stream = new MemoryStream();
            using (var wordDoc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
            {
                var mainPart = wordDoc.AddMainDocumentPart();
                mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document(new Body());
                var body = mainPart.Document.Body!;

                var reportHeader = sections[0];
                body.Append(CreateWinnersWordParagraph(
                    $"งานสัปดาห์วิทยาศาสตร์แห่งชาติ (ส่วนภูมิภาค) ประจำปี {reportHeader.YearNumber}", 32));
                body.Append(CreateWinnersWordParagraph("มหาวิทยาลัยราชภัฏกาญจนบุรี", 32));
                body.Append(CreateWinnersWordParagraph(reportHeader.Date, 32));
                foreach (var section in sections)
                {
                    body.Append(CreateWinnersWordParagraph(
                        $"ผล{section.CompetitionName} ระดับชั้น {section.LevelName}",
                        36, bold: true));
                    body.Append(CreateWinnersWordParagraph(new string('*', 90), 28));

                    var table = new Table(
                        new TableProperties(
                            new TableWidth { Width = "10000", Type = TableWidthUnitValues.Dxa },
                            new TableBorders(
                                new TopBorder { Val = BorderValues.Single, Size = 8 },
                                new BottomBorder { Val = BorderValues.Single, Size = 8 },
                                new LeftBorder { Val = BorderValues.Single, Size = 8 },
                                new RightBorder { Val = BorderValues.Single, Size = 8 },
                                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 8 },
                                new InsideVerticalBorder { Val = BorderValues.Single, Size = 8 })),
                        new TableGrid(
                            new GridColumn { Width = "2000" },
                            new GridColumn { Width = "4000" },
                            new GridColumn { Width = "4000" }));

                    table.Append(new TableRow(
                        new TableRowProperties(new TableHeader()),
                        CreateWinnersWordCell(new[] { "รางวัล" }, 2000, center: true, bold: true, fontSize: 32),
                        CreateWinnersWordCell(new[] { "ชื่อ-นามสกุล" }, 4000, center: true, bold: true, fontSize: 32),
                        CreateWinnersWordCell(new[] { "โรงเรียน" }, 4000, center: true, bold: true, fontSize: 32)));

                    foreach (var group in section.Rows.GroupBy(row => row.Award).OrderBy(group => group.Min(row => row.Order)))
                    {
                        var first = true;
                        foreach (var row in group)
                        {
                            table.Append(new TableRow(
                                CreateWinnersWordCell(first ? row.Award.Split('\n') : Array.Empty<string>(), 2000, center: true, fontSize: 32),
                                CreateWinnersWordCell(row.Names, 4000, fontSize: 24),
                                CreateWinnersWordCell(new[] { row.School }, 4000, fontSize: 26)));
                            first = false;
                        }
                    }

                    body.Append(table);
                }

                body.Append(CreatePortraitSection());
                mainPart.Document.Save();
            }

            return stream.ToArray();
        }

        private static (int Order, string Label) ClassifyWinnersWordAward(string? raw)
        {
            var compact = (raw ?? "").Trim()
                .Replace("๐", "0").Replace("๑", "1").Replace("๒", "2")
                .Replace("๓", "3").Replace("๔", "4").Replace("๕", "5")
                .Replace("๖", "6").Replace("๗", "7").Replace("๘", "8").Replace("๙", "9")
                .Replace("เแข", "แข่งขัน").Replace("เเข่งขัน", "แข่งขัน");
            compact = new string(compact.Where(character => !char.IsWhiteSpace(character)).ToArray());

            if (compact.Length == 0 || compact.Contains("เข้าร่วม")) return (99, "");
            if (compact.Contains("รองชนะเลิศ") || (compact.Contains("ชนะเลิศ") && compact.Contains("อันดับ")))
            {
                if (compact.Contains("อันดับ1") || compact.Contains("อันดับที่1") || compact.Contains("อันดับหนึ่ง"))
                    return (20, "รางวัลรองชนะเลิศ\nอันดับ 1");
                if (compact.Contains("อันดับ2") || compact.Contains("อันดับที่2") || compact.Contains("อันดับสอง"))
                    return (30, "รางวัลรองชนะเลิศ\nอันดับ 2");
                return (25, "รางวัลรองชนะเลิศ");
            }
            if (compact.Contains("ชนะเลิศ")) return (10, "รางวัลชนะเลิศ");
            if (compact.Contains("ชมเชย")) return (40, "รางวัลชมเชย");
            return (50, (raw ?? "").Trim()
                .Replace("เแข", "แข่งขัน").Replace("เเข่งขัน", "แข่งขัน"));
        }

        private static Paragraph CreateWinnersWordParagraph(string text, int fontSize, bool bold = false)
        {
            var properties = new ParagraphProperties();

            properties.Append(new SpacingBetweenLines { Before = "0", After = "0" });
            properties.Append(new Justification { Val = JustificationValues.Center });

            var runProperties = CreateWinnersWordRunProperties(fontSize, bold);
            return new Paragraph(properties,
                new Run(runProperties, new Text(text) { Space = SpaceProcessingModeValues.Preserve }));
        }

        private static TableCell CreateWinnersWordCell(IEnumerable<string> lines, int widthTwips,
            bool center = false, bool bold = false, int fontSize = 26)
        {
            var paragraphProperties = new ParagraphProperties(
                new SpacingBetweenLines { Before = "60", After = "60" },
                new Justification { Val = center ? JustificationValues.Center : JustificationValues.Left });
            var run = new Run(CreateWinnersWordRunProperties(fontSize, bold));
            var first = true;
            foreach (var line in lines)
            {
                if (!first) run.Append(new Break());
                run.Append(new Text(line) { Space = SpaceProcessingModeValues.Preserve });
                first = false;
            }
            if (first) run.Append(new Text("") { Space = SpaceProcessingModeValues.Preserve });

            return new TableCell(
                new TableCellProperties(new TableCellWidth
                {
                    Type = TableWidthUnitValues.Dxa,
                    Width = widthTwips.ToString()
                }),
                new Paragraph(paragraphProperties, run));
        }

        private static RunProperties CreateWinnersWordRunProperties(int fontSize, bool bold)
        {
            var properties = new RunProperties(
                new RunFonts
                {
                    Ascii = "TH Sarabun New",
                    HighAnsi = "TH Sarabun New",
                    EastAsia = "TH Sarabun New",
                    ComplexScript = "TH Sarabun New"
                });
            if (bold)
            {
                properties.Append(new Bold());
                properties.Append(new BoldComplexScript());
            }
            properties.Append(new FontSize { Val = fontSize.ToString() });
            properties.Append(new FontSizeComplexScript { Val = fontSize.ToString() });
            return properties;
        }

        public async Task<byte[]> GenerateTeamReportWordByCompetitionYearAsync(int competitionYearID)
        {
            var teams = await _context.Teams
                .Where(t => t.CompetitionLevel!.CompetitionYearID == competitionYearID)
                .Where(t => t.Status == SD.TeamStatus.CheckedIn || t.Status == SD.TeamStatus.Verified)
                .Include(t => t.Students)
                .Include(t => t.CompetitionLevel).ThenInclude(cl => cl.CompetitionYear).ThenInclude(cy => cy.Competition)
                .Include(t => t.CompetitionLevel).ThenInclude(cl => cl.CompetitionYear).ThenInclude(cy => cy.Year)
                .Include(t => t.CompetitionLevel).ThenInclude(cl => cl.EducationLevel)
                .Include(t => t.schools)
                .OrderBy(t => t.CompetitionLevel!.EducationLevel!.LevelName)
                .ThenBy(t => t.schools!.school_name)
                .ToListAsync();

            if (!teams.Any())
                throw new KeyNotFoundException("ไม่พบทีมในกิจกรรมนี้");

            bool includeProjectColumn = teams.Any(t => !string.IsNullOrWhiteSpace(t.ProjectName));

            using var stream = new MemoryStream();

            using (var wordDoc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
            {
                var mainPart = wordDoc.AddMainDocumentPart();
                mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document();

                var body = new Body();

                var competitionName = teams.First().CompetitionLevel?
                    .CompetitionYear?
                    .Competition?
                    .CompetitionName ?? "";

                var yearName = teams.First().CompetitionLevel?
                    .CompetitionYear?
                    .Year?
                    .YearName ?? "";

              

                var levelGroups = teams
                    .GroupBy(t => t.CompetitionLevelID)
                    .OrderBy(g => g.First().CompetitionLevel?.EducationLevel?.LevelName)
                    .ToList();

                foreach (var levelGroup in levelGroups)
                {
                    var levelName = levelGroup.First().CompetitionLevel?
                        .EducationLevel?
                        .LevelName ?? "";

                    body.Append(new Paragraph(
                      new ParagraphProperties(
                          new Justification { Val = JustificationValues.Center },
                          new SpacingBetweenLines { Before = "250", After = "150" }
                      ),
                      new Run(
                          CreateFontRunProperties(40, bold: true),
                          new Text($"การแข่งขัน{competitionName} ระดับ{levelName}")
                      )
                  ));

                    var table = new Table();

                    var tblProps = new TableProperties(
                        new TableWidth { Type = TableWidthUnitValues.Pct, Width = "5000" },
                        new TableBorders(
                            new TopBorder { Val = BorderValues.Single, Size = 6 },
                            new BottomBorder { Val = BorderValues.Single, Size = 6 },
                            new LeftBorder { Val = BorderValues.Single, Size = 6 },
                            new RightBorder { Val = BorderValues.Single, Size = 6 },
                            new InsideHorizontalBorder { Val = BorderValues.Single, Size = 6 },
                            new InsideVerticalBorder { Val = BorderValues.Single, Size = 6 }
                        )
                    );

                    table.AppendChild(tblProps);

                    table.AppendChild(CreateTableGrid("700", "2000", "3000", "2800", "1000"));

                    var headerRow = new TableRow();

                    headerRow.Append(
                        CreateHeaderCell("ลำดับ", "700"),
                        CreateHeaderCell("โรงเรียน", "2000")
                    );

                    headerRow.Append(
                        CreateHeaderCell("ผู้สมัครแข่งขัน", "3000"),
                        CreateHeaderCell("ครูผู้ควบคุม", "2800"),
                        CreateHeaderCell("หมายเหตุ", "1000")
                    );

                    table.Append(headerRow);

                    int index = 1;

                    foreach (var team in levelGroup)
                    {
                        var schoolNameRaw = string.IsNullOrWhiteSpace(team.schools?.school_name)
                            ? "—"
                            : team.schools.school_name;

                        // The trailing 3 is part of this school's official branch name.
                        var schoolName = schoolNameRaw == "โรงเรียนบรรหารแจ่มใสวิทยา 3"
                            ? schoolNameRaw
                            : Regex.Replace(schoolNameRaw, @"\s*[\d๐-๙]+$", "");

                        var studentList = team.Students
                            .Where(s => HasExportableStudentName(s.FirstName, s.LastName))
                            .Select((s, i) =>
                                $"{i + 1}. {PrefixToString(s.TitlePrefix)} {s.FirstName} {s.LastName}");

                        var advisorLines = new List<string>();

                        var primaryAdvisor = FormatPerson(
                            team.PrimaryTitlePrefix,
                            team.PrimaryFirstName,
                            team.PrimaryLastName
                        );

                        if (!string.IsNullOrWhiteSpace(primaryAdvisor))
                        {
                            advisorLines.Add(primaryAdvisor);
                        }

                        if (!string.IsNullOrWhiteSpace(team.SecondaryFirstName) ||
                            !string.IsNullOrWhiteSpace(team.SecondaryLastName))
                        {
                            var secondaryAdvisor = FormatPerson(
                                team.SecondaryTitlePrefix,
                                team.SecondaryFirstName,
                                team.SecondaryLastName
                            );

                            if (!string.IsNullOrWhiteSpace(secondaryAdvisor))
                            {
                                advisorLines.Add(secondaryAdvisor);
                            }
                        }

                        var project = includeProjectColumn ? team.ProjectName?.Trim() : null;
                        var hasProject = !string.IsNullOrEmpty(project);

                        var row = new TableRow();

                        row.Append(
                            MergeDown(CreateTableCell(index++.ToString(), "700"), hasProject, restart: true),
                            CreateTableCell(schoolName, "2000")
                        );

                        row.Append(
                            CreateParagraphCell(studentList, "3000"),
                            CreateParagraphCell(advisorLines, "2800"),
                            CreateTableCell("", "1000")
                        );

                        table.Append(row);
                        if (hasProject)
                            table.Append(CreateProjectRow(project!, "700", "8800", 4));
                    }

                    body.Append(table);
                }

                body.Append(CreatePortraitSection());

                mainPart.Document.Append(body);
            }

            return stream.ToArray();
        }

        public async Task<byte[]> GenerateTeamReportWordByCompetitionLevelsAsync(List<int> competitionLevelIds)
        {
            var normalizedIds = competitionLevelIds
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (!normalizedIds.Any())
                throw new KeyNotFoundException("ไม่พบระดับชั้นที่เลือก");

            var teams = await _context.Teams
                .Where(t => t.CompetitionLevelID.HasValue && normalizedIds.Contains(t.CompetitionLevelID.Value))
                .Where(t => t.Status == SD.TeamStatus.CheckedIn || t.Status == SD.TeamStatus.Verified)
                .Include(t => t.Students)
                .Include(t => t.CompetitionLevel).ThenInclude(cl => cl.CompetitionYear).ThenInclude(cy => cy.Competition)
                .Include(t => t.CompetitionLevel).ThenInclude(cl => cl.CompetitionYear).ThenInclude(cy => cy.Year)
                .Include(t => t.CompetitionLevel).ThenInclude(cl => cl.EducationLevel)
                .Include(t => t.schools)
                .OrderBy(t => t.CompetitionLevel!.CompetitionYear!.Competition!.CompetitionName)
                .ThenBy(t => t.CompetitionLevel!.EducationLevel!.LevelName)
                .ThenBy(t => t.schools!.school_name)
                .ToListAsync();

            if (!teams.Any())
                throw new KeyNotFoundException("ไม่พบทีมในระดับชั้นที่เลือก");

            bool includeProjectColumn = teams.Any(t => !string.IsNullOrWhiteSpace(t.ProjectName));

            using var stream = new MemoryStream();

            using (var wordDoc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
            {
                var mainPart = wordDoc.AddMainDocumentPart();
                mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document();

                var body = new Body();

                var levelGroups = teams
                    .GroupBy(t => t.CompetitionLevelID)
                    .OrderBy(g => g.First().CompetitionLevel?.CompetitionYear?.Competition?.CompetitionName)
                    .ThenBy(g => g.First().CompetitionLevel?.EducationLevel?.LevelName)
                    .ToList();

                foreach (var levelGroup in levelGroups)
                {
                    var firstTeam = levelGroup.First();
                    var competitionName = firstTeam.CompetitionLevel?.CompetitionYear?.Competition?.CompetitionName ?? "";
                    var levelName = firstTeam.CompetitionLevel?.EducationLevel?.LevelName ?? "";

                    body.Append(new Paragraph(
                      new ParagraphProperties(
                          new Justification { Val = JustificationValues.Center },
                          new SpacingBetweenLines { Before = "250", After = "150" }
                      ),
                      new Run(
                          CreateFontRunProperties(40, bold: true),
                          new Text($"การแข่งขัน{competitionName} ระดับ{levelName}")
                      )
                  ));

                    var table = new Table();

                    var tblProps = new TableProperties(
                        new TableWidth { Type = TableWidthUnitValues.Pct, Width = "5000" },
                        new TableBorders(
                            new TopBorder { Val = BorderValues.Single, Size = 6 },
                            new BottomBorder { Val = BorderValues.Single, Size = 6 },
                            new LeftBorder { Val = BorderValues.Single, Size = 6 },
                            new RightBorder { Val = BorderValues.Single, Size = 6 },
                            new InsideHorizontalBorder { Val = BorderValues.Single, Size = 6 },
                            new InsideVerticalBorder { Val = BorderValues.Single, Size = 6 }
                        )
                    );

                    table.AppendChild(tblProps);

                    table.AppendChild(CreateTableGrid("700", "2000", "3000", "2800", "1000"));

                    var headerRow = new TableRow();

                    headerRow.Append(
                        CreateHeaderCell("ลำดับ", "700"),
                        CreateHeaderCell("โรงเรียน", "2000")
                    );

                    headerRow.Append(
                        CreateHeaderCell("ผู้สมัครแข่งขัน", "3000"),
                        CreateHeaderCell("ครูผู้ควบคุม", "2800"),
                        CreateHeaderCell("หมายเหตุ", "1000")
                    );

                    table.Append(headerRow);

                    int index = 1;

                    foreach (var team in levelGroup)
                    {
                        var schoolNameRaw = string.IsNullOrWhiteSpace(team.schools?.school_name)
                            ? "—"
                            : team.schools.school_name;

                        // The trailing 3 is part of this school's official branch name.
                        var schoolName = schoolNameRaw == "โรงเรียนบรรหารแจ่มใสวิทยา 3"
                            ? schoolNameRaw
                            : Regex.Replace(schoolNameRaw, @"\s*[\d๐-๙]+$", "");

                        var studentList = team.Students
                            .Where(s => HasExportableStudentName(s.FirstName, s.LastName))
                            .Select((s, i) =>
                                $"{i + 1}. {PrefixToString(s.TitlePrefix)} {s.FirstName} {s.LastName}");

                        var advisorLines = new List<string>();

                        var primaryAdvisor = FormatPerson(
                            team.PrimaryTitlePrefix,
                            team.PrimaryFirstName,
                            team.PrimaryLastName
                        );

                        if (!string.IsNullOrWhiteSpace(primaryAdvisor))
                            advisorLines.Add(primaryAdvisor);

                        if (!string.IsNullOrWhiteSpace(team.SecondaryFirstName) ||
                            !string.IsNullOrWhiteSpace(team.SecondaryLastName))
                        {
                            var secondaryAdvisor = FormatPerson(
                                team.SecondaryTitlePrefix,
                                team.SecondaryFirstName,
                                team.SecondaryLastName
                            );

                            if (!string.IsNullOrWhiteSpace(secondaryAdvisor))
                                advisorLines.Add(secondaryAdvisor);
                        }

                        var project = includeProjectColumn ? team.ProjectName?.Trim() : null;
                        var hasProject = !string.IsNullOrEmpty(project);

                        var row = new TableRow();

                        row.Append(
                            MergeDown(CreateTableCell(index++.ToString(), "700"), hasProject, restart: true),
                            CreateTableCell(schoolName, "2000")
                        );

                        row.Append(
                            CreateParagraphCell(studentList, "3000"),
                            CreateParagraphCell(advisorLines, "2800"),
                            CreateTableCell("", "1000")
                        );

                        table.Append(row);
                        if (hasProject)
                            table.Append(CreateProjectRow(project!, "700", "8800", 4));
                    }

                    body.Append(table);
                }

                body.Append(CreatePortraitSection());

                mainPart.Document.Append(body);
            }

            return stream.ToArray();
        }

        public async Task<byte[]> GenerateCompetitionYearSummaryWordAsync(int yearId)
        {
            var year = await _context.Years
                .AsNoTracking()
                .FirstOrDefaultAsync(y => y.YearID == yearId);

            if (year == null)
                throw new KeyNotFoundException("ไม่พบปีที่ต้องการ");

            var competitionYears = await _context.CompetitionYears
                .AsNoTracking()
                .Include(cy => cy.Competition)
                .Where(cy => cy.YearID == yearId)
                .OrderBy(cy => cy.Competition!.CompetitionName)
                .ToListAsync();

            if (!competitionYears.Any())
                throw new KeyNotFoundException("ไม่พบกิจกรรมในปีที่เลือก");

            var competitionYearIds = competitionYears
                .Select(cy => cy.CompetitionYearID)
                .ToList();

            var competitionLevels = await _context.CompetitionLevels
                .AsNoTracking()
                .Include(cl => cl.EducationLevel)
                .Where(cl => competitionYearIds.Contains(cl.CompetitionYearID))
                .OrderBy(cl => cl.CompetitionYearID)
                .ThenBy(cl => cl.EducationLevel!.LevelName)
                .ToListAsync();

            var teams = await _context.Teams
                .AsNoTracking()
                .Where(t => t.CompetitionLevelID.HasValue &&
                            t.CompetitionLevel != null &&
                            competitionYearIds.Contains(t.CompetitionLevel.CompetitionYearID))
                .Include(t => t.Students)
                .Include(t => t.CompetitionLevel)
                .ToListAsync();

            using var stream = new MemoryStream();

            using (var wordDoc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
            {
                var mainPart = wordDoc.AddMainDocumentPart();
                mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document();

                var body = new Body();

                body.Append(new Paragraph(
                    new ParagraphProperties(
                        new Justification { Val = JustificationValues.Center },
                        new SpacingBetweenLines { Before = "200", After = "200" }
                    ),
                    new Run(
                        CreateFontRunProperties(40, bold: true),
                        new Text($"รายงานสรุปผู้สมัครรายกิจกรรม {year.YearName} ")
                    )   
                ));

                body.Append(new Paragraph(
                    new ParagraphProperties(
                        new Justification { Val = JustificationValues.Center },
                        new SpacingBetweenLines { After = "250" }
                    )            
                ));

                var summaryTable = new Table();
                var summaryTableProps = new TableProperties(
                    new TableWidth { Type = TableWidthUnitValues.Pct, Width = "5000" },
                    new TableBorders(
                        new TopBorder { Val = BorderValues.Single, Size = 6 },
                        new BottomBorder { Val = BorderValues.Single, Size = 6 },
                        new LeftBorder { Val = BorderValues.Single, Size = 6 },
                        new RightBorder { Val = BorderValues.Single, Size = 6 },
                        new InsideHorizontalBorder { Val = BorderValues.Single, Size = 6 },
                        new InsideVerticalBorder { Val = BorderValues.Single, Size = 6 }
                    )
                );

                summaryTable.AppendChild(summaryTableProps);

                var summaryHeaderRow = new TableRow();
                summaryHeaderRow.Append(
                    CreateHeaderCell("ลำดับ", "800"),
                    CreateHeaderCell("กิจกรรม", "2500"),
                    CreateHeaderCell("รายการ", "2300"),
                    CreateHeaderCell("โรงเรียน", "1000"),
                    CreateHeaderCell("ทีม", "1000"),
                    CreateHeaderCell("นักเรียน", "1000")
                );
                summaryTable.Append(summaryHeaderRow);

                int compIndex = 1;

                foreach (var competitionYear in competitionYears)
                {
                    var competitionName = competitionYear.Competition?.CompetitionName ?? "-";
                    var competitionTeams = teams
                        .Where(t => t.CompetitionLevel?.CompetitionYearID == competitionYear.CompetitionYearID)
                        .ToList();

                    var levels = competitionLevels
                        .Where(cl => cl.CompetitionYearID == competitionYear.CompetitionYearID)
                        .OrderBy(cl => cl.EducationLevel?.LevelName)
                        .ToList();

                    var row = new TableRow();
                    row.Append(
                        CreateTableCell((compIndex++).ToString(), "800"),
                        CreateTableCell(competitionName, "2500"),
                        CreateTableCell($"สรุปยอดรวมทั้งหมด", "2300"),
                        CreateTableCell(competitionTeams.Where(t => t.school_code.HasValue && t.CompetitionLevelID.HasValue).Select(t => new { t.CompetitionLevelID, t.school_code }).Distinct().Count().ToString(), "1000"),
                        CreateTableCell(competitionTeams.Count.ToString(), "1000"),
                        CreateTableCell(competitionTeams.Sum(t => t.Students?.Count ?? 0).ToString(), "1000")
                    );
                    summaryTable.Append(row);

                    foreach (var level in levels)
                    {
                        var levelTeams = competitionTeams
                            .Where(t => t.CompetitionLevelID == level.CompetitionLevelID)
                            .ToList();

                        var levelRow = new TableRow();
                        levelRow.Append(
                            CreateTableCell("", "800"),
                            CreateTableCell("", "2500"),
                            CreateTableCell(level.EducationLevel?.LevelName ?? "-", "2300"),
                            CreateTableCell(levelTeams.Where(t => t.school_code.HasValue).Select(t => t.school_code!.Value).Distinct().Count().ToString(), "1000"),
                            CreateTableCell(levelTeams.Count.ToString(), "1000"),
                            CreateTableCell(levelTeams.Sum(t => t.Students?.Count ?? 0).ToString(), "1000")
                        );
                        summaryTable.Append(levelRow);
                    }

                    if (!levels.Any())
                    {
                        var emptyRow = new TableRow();
                        emptyRow.Append(
                            CreateTableCell("", "800"),
                            CreateTableCell("", "2500"),
                            CreateTableCell("ไม่พบข้อมูลระดับชั้น", "2300"),
                            CreateTableCell("0", "1000"),
                            CreateTableCell("0", "1000"),
                            CreateTableCell("0", "1000")
                        );
                        summaryTable.Append(emptyRow);
                    }
                }

                var totalSchools = competitionYears.Sum(cy => 
                    teams.Where(t => t.CompetitionLevel != null && 
                                     t.CompetitionLevel.CompetitionYearID == cy.CompetitionYearID && 
                                     t.CompetitionLevelID.HasValue && 
                                     t.school_code.HasValue)
                         .Select(t => new { t.CompetitionLevelID, t.school_code })
                         .Distinct()
                         .Count()); 
                var totalTeams = teams.Count;
                var totalStudents = teams.Sum(t => t.Students?.Count ?? 0);

                var grandTotalRow = new TableRow();
                grandTotalRow.Append(
                    CreateTableCell("", "800"),
                    CreateTableCell("ผลรวมทั้งหมด", "2500"),
                    CreateTableCell("", "2300"),
                    CreateTableCell(totalSchools.ToString(), "1000"),
                    CreateTableCell(totalTeams.ToString(), "1000"),
                    CreateTableCell(totalStudents.ToString(), "1000")
                );
                summaryTable.Append(grandTotalRow);

                body.Append(summaryTable);
                body.Append(CreatePortraitSection());
                mainPart.Document.Append(body);
            }

            return stream.ToArray();
        }

        public async Task<byte[]> GenerateCompetitionYearSummaryPdfAsync(int yearId)
        {
            var year = await _context.Years
                .AsNoTracking()
                .FirstOrDefaultAsync(y => y.YearID == yearId);

            if (year == null)
                throw new KeyNotFoundException("ไม่พบปีที่ต้องการ");

            var competitionSummaries = await _context.CompetitionYears
                .AsNoTracking()
                .Include(cy => cy.Competition)
                .Where(cy => cy.YearID == yearId)
                .OrderBy(cy => cy.Competition!.CompetitionName)
                .Select(cy => new
                {
                    CompetitionName = cy.Competition!.CompetitionName,
                    SchoolCount = _context.Teams
                        .Where(t => t.CompetitionLevel != null &&
                                    t.CompetitionLevel.CompetitionYearID == cy.CompetitionYearID &&
                                    t.CompetitionLevelID.HasValue &&
                                    t.school_code.HasValue)
                        .Select(t => new
                        {
                            CompetitionLevelID = t.CompetitionLevelID!.Value,
                            SchoolCode = t.school_code!.Value
                        })
                        .Distinct()
                        .Count(),
                    StudentCount = _context.Teams
                        .Where(t => t.CompetitionLevel != null &&
                                    t.CompetitionLevel.CompetitionYearID == cy.CompetitionYearID)
                        .SelectMany(t => t.Students)
                        .Count()
                })
                .ToListAsync();

            if (!competitionSummaries.Any())
                throw new KeyNotFoundException("ไม่พบกิจกรรมในปีที่เลือก");

            EnsureFontRegistered();
            QuestPDF.Settings.License = LicenseType.Community;

            using var stream = new MemoryStream();

            QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(24);
                    page.DefaultTextStyle(x => x.FontSize(16).FontFamily("TH Sarabun New"));

                    page.Header()
                        .PaddingBottom(12)
                        .AlignCenter()
                        .Text($"สรุปกิจกรรม {year.YearName}")
                        .FontSize(22)
                        .SemiBold();

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(4);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCellStyle).Text("ชื่อกิจกรรม").SemiBold().AlignCenter();
                            header.Cell().Element(HeaderCellStyle).Text("จำนวนโรงเรียน").SemiBold().AlignCenter();
                            header.Cell().Element(HeaderCellStyle).Text("จำนวนนักเรียน").SemiBold().AlignCenter();
                        });

                        foreach (var item in competitionSummaries)
                        {
                            table.Cell().Element(CellStyle).Text(item.CompetitionName);
                            table.Cell().Element(CellStyle).AlignCenter().Text(item.SchoolCount.ToString());
                            table.Cell().Element(CellStyle).AlignCenter().Text(item.StudentCount.ToString());
                        }
                    });

                    page.Footer().AlignRight().Text(x =>
                    {
                        x.Span("หน้าที่ ");
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                });
            }).GeneratePdf(stream);

            return stream.ToArray();

            IContainer CellStyle(IContainer c) => c.Border(1).Padding(6).AlignLeft().AlignMiddle();
            IContainer HeaderCellStyle(IContainer c) => c.Border(1).Background("#E2E8F0").Padding(6);
        }

        public async Task<byte[]> GenerateCompetitionAttendanceReportPdfAsync(int yearId)
        {
            var year = await _context.Years
                .AsNoTracking()
                .FirstOrDefaultAsync(y => y.YearID == yearId);

            if (year == null)
                throw new KeyNotFoundException("ไม่พบปีที่ต้องการ");

            var competitionYears = await _context.CompetitionYears
                .AsNoTracking()
                .Include(cy => cy.Competition)
                .Where(cy => cy.YearID == yearId && cy.IsActive)
                .OrderBy(cy => cy.Competition!.CompetitionName)
                .ToListAsync();

            var competitionYearIds = competitionYears.Select(cy => cy.CompetitionYearID).ToList();
            var competitionLevels = await _context.CompetitionLevels
                .AsNoTracking()
                .Include(cl => cl.EducationLevel)
                .Where(cl => competitionYearIds.Contains(cl.CompetitionYearID))
                .ToListAsync();
            var competitionLevelIds = competitionLevels.Select(cl => cl.CompetitionLevelID).ToHashSet();
            var teams = await _context.Teams
                .AsNoTracking()
                .Include(t => t.Students)
                .Where(t => t.CompetitionLevelID.HasValue && competitionLevelIds.Contains(t.CompetitionLevelID.Value))
                .ToListAsync();

            if (!competitionYears.Any())
                throw new KeyNotFoundException("ไม่พบกิจกรรมในปีที่เลือก");

            var registeredTeams = teams.Where(t => t.Status != TeamStatus.Disqualified).ToList();
            var checkedInTeams = registeredTeams.Where(t => t.Status == TeamStatus.CheckedIn).ToList();

            EnsureFontRegistered();
            QuestPDF.Settings.License = LicenseType.Community;

            using var stream = new MemoryStream();

            QuestPDF.Fluent.Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(22);
                    page.DefaultTextStyle(x => x.FontSize(13).FontFamily("TH Sarabun New"));

                    page.Header().Column(column =>
                    {
                        column.Item().AlignCenter().Text($"รายงานผู้สมัครและผู้ลงทะเบียนเข้างานการแข่งขันสัปดาห์วิทยาศาสตร์ {year.YearName}")
                            .FontSize(21).SemiBold();
                        column.Item().PaddingTop(2).AlignCenter().Text($"จัดทำเมื่อ {DateTime.Now:dd/MM/yyyy HH:mm} น.")
                            .FontSize(11).FontColor("#475569");
                    });

                    page.Content().PaddingVertical(12).Column(column =>
                    {
                        column.Spacing(10);
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                            });

                            SummaryCell(table, "กิจกรรม", competitionYears.Count.ToString());
                            SummaryCell(table, "ทีมลงทะเบียนเข้าร่วมงาน", checkedInTeams.Count.ToString());
                            SummaryCell(table, "นักเรียนเข้าร่วมงาน", checkedInTeams.Sum(t => t.Students?.Count ?? 0).ToString());
                        });

                        column.Item().Text("สรุปทีมและนักเรียนที่ลงทะเบียนเข้าร่วมงานรายกิจกรรมและระดับชั้น").FontSize(16).SemiBold();
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(5);
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(3);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Border(1).BorderColor("#94A3B8").Background("#1E293B").Padding(5).AlignCenter().Text("กิจกรรม / ระดับชั้น").FontColor("#FFFFFF").SemiBold();
                                header.Cell().Border(1).BorderColor("#94A3B8").Background("#1E293B").Padding(5).AlignCenter().Text("ทีมลงทะเบียนเข้าร่วมงาน").FontColor("#FFFFFF").SemiBold();
                                header.Cell().Border(1).BorderColor("#94A3B8").Background("#1E293B").Padding(5).AlignCenter().Text("นักเรียนเข้าร่วมงาน").FontColor("#FFFFFF").SemiBold();
                            });

                            foreach (var competitionYear in competitionYears)
                            {
                                var levelIds = competitionLevels
                                    .Where(cl => cl.CompetitionYearID == competitionYear.CompetitionYearID)
                                    .Select(cl => cl.CompetitionLevelID)
                                    .ToHashSet();
                                var activityTeams = registeredTeams
                                    .Where(t => t.CompetitionLevelID.HasValue && levelIds.Contains(t.CompetitionLevelID.Value))
                                    .ToList();
                                var activityCheckedInTeams = activityTeams
                                    .Where(t => t.Status == TeamStatus.CheckedIn)
                                    .ToList();

                                DataCell(table, competitionYear.Competition?.CompetitionName ?? "-", true);
                                DataCell(table, activityCheckedInTeams.Count.ToString(), true, true);
                                DataCell(table, activityCheckedInTeams.Sum(t => t.Students?.Count ?? 0).ToString(), true, true);

                                foreach (var level in competitionLevels.Where(cl => cl.CompetitionYearID == competitionYear.CompetitionYearID).OrderBy(cl => cl.EducationLevel!.LevelName))
                                {
                                    var levelTeams = activityTeams.Where(t => t.CompetitionLevelID == level.CompetitionLevelID).ToList();
                                    var levelCheckedInTeams = levelTeams.Where(t => t.Status == TeamStatus.CheckedIn).ToList();

                                    DataCell(table, $"   {level.EducationLevel?.LevelName ?? "-"}");
                                    DataCell(table, levelCheckedInTeams.Count.ToString(), false, true);
                                    DataCell(table, levelCheckedInTeams.Sum(t => t.Students?.Count ?? 0).ToString(), false, true);
                                }
                            }
                        });
                    });

                    page.Footer().AlignRight().Text(text =>
                    {
                        text.Span("หน้าที่ ");
                        text.CurrentPageNumber();
                        text.Span(" / ");
                        text.TotalPages();
                    });
                });
            }).GeneratePdf(stream);

            return stream.ToArray();

            static void SummaryCell(TableDescriptor table, string label, string value)
            {
                table.Cell().Border(1).BorderColor("#CBD5E1").Background("#F8FAFC").Padding(7).Column(column =>
                {
                    column.Item().Text(label).FontSize(11).FontColor("#475569");
                    column.Item().Text(value).FontSize(18).SemiBold().FontColor("#0F172A");
                });
            }

            static void DataCell(TableDescriptor table, string text, bool bold = false, bool center = false)
            {
                var cell = table.Cell().Border(1).BorderColor("#CBD5E1").Padding(5).AlignMiddle();
                if (center) cell = cell.AlignCenter();
                var content = cell.Text(text);
                if (bold) content.SemiBold();
            }
        }

        /* ================= Helpers ================= */


        private static TableCell CreateHeaderCell(string text, string widthTwips)
        {
            var para = new Paragraph(
                new ParagraphProperties(
                    new Justification { Val = JustificationValues.Center },
                    new SpacingBetweenLines { Before = "60", After = "60" }
                ),
                new Run(
                    new RunProperties(
                        new RunFonts
                        {
                            Ascii = "TH Sarabun New",
                            HighAnsi = "TH Sarabun New",
                            EastAsia = "TH Sarabun New",
                            ComplexScript = "TH Sarabun New"
                        },
                        new FontSize { Val = "36" }, // 18 pt
                        new Bold()
                    ),
                    new Text(text ?? "") { Space = SpaceProcessingModeValues.Preserve }
                )
            );

            return new TableCell(para)
            {
                TableCellProperties = new TableCellProperties(
                    new TableCellWidth { Type = TableWidthUnitValues.Dxa, Width = widthTwips }
                )
            };
        }
        // ความกว้างคอลัมน์ของตาราง Word (ต้องมีเพื่อให้ gridSpan แสดงถูกใน Word)
        private static TableGrid CreateTableGrid(params string[] widthsTwips) =>
            new(widthsTwips.Select(w => new GridColumn { Width = w }));

        // รวมช่องแนวตั้ง (ใช้กับ "ลำดับ" ให้คลุมทั้งแถวทีมและแถวชื่อโครงงาน)
        private static TableCell MergeDown(TableCell cell, bool merge, bool restart)
        {
            if (merge)
                cell.TableCellProperties!.Append(new VerticalMerge { Val = restart ? MergedCellValues.Restart : MergedCellValues.Continue });
            return cell;
        }

        // แถวชื่อโครงงาน: ช่องลำดับต่อจากแถวบน + ช่องกว้างเต็มที่เหลือ
        private static TableRow CreateProjectRow(string project, string indexWidth, string spanWidth, int span)
        {
            var cell = new TableCell(
                new TableCellProperties(
                    new TableCellWidth { Type = TableWidthUnitValues.Dxa, Width = spanWidth },
                    new GridSpan { Val = span },
                    new Shading { Val = ShadingPatternValues.Clear, Color = "auto", Fill = "F3F4F6" }
                ),
                new Paragraph(
                    new ParagraphProperties(new SpacingBetweenLines { Before = "40", After = "40" }),
                    new Run(ThaiRunProps(bold: true), new Text("ชื่อโครงงาน: ") { Space = SpaceProcessingModeValues.Preserve }),
                    new Run(ThaiRunProps(bold: false), new Text(project) { Space = SpaceProcessingModeValues.Preserve })
                )
            );
            return new TableRow(MergeDown(CreateTableCell("", indexWidth), true, restart: false), cell);
        }

        private static RunProperties ThaiRunProps(bool bold)
        {
            var p = new RunProperties(
                new RunFonts { Ascii = "TH Sarabun New", HighAnsi = "TH Sarabun New", EastAsia = "TH Sarabun New", ComplexScript = "TH Sarabun New" },
                new FontSize { Val = "32" } // 16 pt
            );
            if (bold) p.Append(new Bold());
            return p;
        }

        private static TableCell CreateTableCell(string text, string widthTwips)
        {
            var para = new Paragraph(
                new ParagraphProperties(
                    new SpacingBetweenLines { Before = "60", After = "60" }
                ),
                new Run(
                    new RunProperties(
                        new RunFonts
                        {
                            Ascii = "TH Sarabun New",
                            HighAnsi = "TH Sarabun New",
                            EastAsia = "TH Sarabun New",
                            ComplexScript = "TH Sarabun New"
                        },
                        new FontSize { Val = "32" } // 16 pt
                    ),
                    new Text(text ?? "") { Space = SpaceProcessingModeValues.Preserve }
                )
            );

            return new TableCell(para)
            {
                TableCellProperties = new TableCellProperties(
                    new TableCellWidth { Type = TableWidthUnitValues.Dxa, Width = widthTwips }
                )
            };
        }
        private static TableCell CreateParagraphCell(IEnumerable<string> lines, string widthTwips)
        {
            var run = new Run(
                new RunProperties(
                    new RunFonts
                    {
                        Ascii = "TH Sarabun New",
                        HighAnsi = "TH Sarabun New",
                        EastAsia = "TH Sarabun New",
                        ComplexScript = "TH Sarabun New"
                    },
                    new FontSize { Val = "32" } // 16 pt
                )
            );

            bool first = true;
            foreach (var line in lines ?? Enumerable.Empty<string>())
            {
                if (!first) run.Append(new Break());
                run.Append(new Text(line ?? "") { Space = SpaceProcessingModeValues.Preserve });
                first = false;
            }

            var para = new Paragraph(
                new ParagraphProperties(
                    new SpacingBetweenLines { Before = "60", After = "60" }
                ),
                run
            );

            return new TableCell(para)
            {
                TableCellProperties = new TableCellProperties(
                    new TableCellWidth { Type = TableWidthUnitValues.Dxa, Width = widthTwips }
                )
            };
        }
        private RunProperties CreateFontRunProperties(int fontSize, bool bold = false)
        {
            var runProps = new RunProperties();

            runProps.Append(new RunFonts
            {
                Ascii = "TH SarabunPSK",
                HighAnsi = "TH SarabunPSK",
                ComplexScript = "TH SarabunPSK",
                EastAsia = "TH SarabunPSK"
            });

            runProps.Append(new FontSize { Val = fontSize.ToString() });
            runProps.Append(new FontSizeComplexScript { Val = fontSize.ToString() });

            if (bold)
            {
                runProps.Append(new Bold());
                runProps.Append(new BoldComplexScript());
            }

            return runProps;
        }
        private static bool HasExportableStudentName(string? firstName, string? lastName)
        {
            static bool IsPlaceholder(string? value)
            {
                var normalized = value?.Trim();
                return string.IsNullOrWhiteSpace(normalized) || normalized == "-";
            }

            // คำนำหน้าไม่เกี่ยวกับความถูกต้องของชื่อ: ตัดเฉพาะรายการที่
            // ทั้งชื่อและนามสกุลไม่มีข้อมูลจริง เช่น "เด็กหญิง - -".
            return !IsPlaceholder(firstName) || !IsPlaceholder(lastName);
        }

        static string? FormatPerson(TitlePrefix prefix, string? first, string? last)
        {
            var title = prefix != TitlePrefix.Other ? prefix.ToString() : "";
            var parts = new[] { title, first, last }
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s!.Trim());

            var full = string.Join(" ", parts).Trim();
            return string.IsNullOrWhiteSpace(full) ? null : full;
        }
        private string PrefixToString(TitlePrefix prefix, string? otherValue = "")
        {
            return prefix switch
            {
                TitlePrefix.นาย => "นาย",
                TitlePrefix.นาง => "นาง",
                TitlePrefix.นางสาว => "นางสาว",
                TitlePrefix.เด็กชาย => "เด็กชาย",
                TitlePrefix.เด็กหญิง => "เด็กหญิง",
                TitlePrefix.ว่าที่ร้อยตรี => "ว่าที่ร้อยตรี",
                TitlePrefix.ว่าที่ร้อยตรีหญิง => "ว่าที่ร้อยตรีหญิง",
                TitlePrefix.Other => string.IsNullOrWhiteSpace(otherValue) ? "" : otherValue,
                _ => ""
            };
        }
        private async Task<byte[]?> MakeSignatureTransparentAsync(byte[]? raw)
        {
            if (raw == null || raw.Length == 0) return null;

            using var img = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(raw);

            // แปลงพื้นหลังขาว/เกือบขาวให้โปร่งใส
            img.ProcessPixelRows(a =>
            {
                for (int y = 0; y < a.Height; y++)
                {
                    var row = a.GetRowSpan(y);
                    for (int x = 0; x < row.Length; x++)
                    {
                        ref var p = ref row[x];
                        // ถ้าเป็นสีเกือบขาว (threshold ปรับได้)
                        if (p.R > 245 && p.G > 245 && p.B > 245)
                            p.A = 0;
                    }
                }
            });

            // ครอปขอบโปร่งใสออกให้กระชับ
            var rect = CropToContent(img);
            if (rect != Rectangle.Empty)
                img.Mutate(m => m.Crop(rect));

            using var ms = new MemoryStream();
            await img.SaveAsPngAsync(ms);
            return ms.ToArray();
        }
        private static Rectangle CropToContent(SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32> img)
        {
            int minX = img.Width, minY = img.Height, maxX = -1, maxY = -1;

            img.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    var row = accessor.GetRowSpan(y);
                    for (int x = 0; x < row.Length; x++)
                    {
                        var p = row[x];
                        if (p.A > 5)
                        {
                            if (x < minX) minX = x;
                            if (y < minY) minY = y;
                            if (x > maxX) maxX = x;
                            if (y > maxY) maxY = y;
                        }
                    }
                }
            });

            if (maxX < minX || maxY < minY) return Rectangle.Empty;

            int pad = 2;
            minX = Math.Max(0, minX - pad);
            minY = Math.Max(0, minY - pad);
            maxX = Math.Min(img.Width - 1, maxX + pad);
            maxY = Math.Min(img.Height - 1, maxY + pad);

            return new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }
        private SectionProperties CreateLandscapeSection()
        {
            return new SectionProperties(
                new PageSize
                {
                    Width = 16838,
                    Height = 11906,
                    Orient = PageOrientationValues.Landscape
                },
                new PageMargin
                {
                    Top = 720,
                    Right = 720,
                    Bottom = 720,
                    Left = 720,
                    Header = 450,
                    Footer = 450,
                    Gutter = 0
                }
            );
        }

        private SectionProperties CreatePortraitSection()
        {
            return new SectionProperties(
                new PageSize
                {
                    Width = 11906,
                    Height = 16838
                },
                new PageMargin
                {
                    Top = 720,
                    Right = 720,
                    Bottom = 720,
                    Left = 720,
                    Header = 450,
                    Footer = 450,
                    Gutter = 0
                }
            );
        }
    }

}
