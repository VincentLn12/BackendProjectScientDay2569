using AutoMapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Moq;
using ProjectScientDay2568.Data;
using ProjectScientDay2568.Mappings;
using ProjectScientDay2568.Models;
using ProjectScientDay2568.Service;

namespace ProjectScientDay2568.Tests.Services;

/// <summary>เกียรติบัตรผู้เข้าอบรม: ใช้การตั้งค่าของปี, คนละหน้า, กันคนอื่น/ผู้ที่ยกเลิก</summary>
public class TrainingCertificateTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ExportService _export;

    public TrainingCertificateTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"TrainingCertificateTests_{Guid.NewGuid()}")
            .Options;
        _context = new ApplicationDbContext(options);
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();

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

        _context.Years.Add(new Year { YearID = 1, YearNumber = 2569, YearName = "2569", DateName = "18-20 สิงหาคม" });
        _context.Trainingtopics.AddRange(
            new Trainingtopic { Id = 1, Name = "การใช้ AI ช่วยสอนวิทยาศาสตร์", YearID = 1 },
            new Trainingtopic { Id = 2, Name = "ไม่ระบุปี" });
        _context.TrainingRegistrations.AddRange(
            new TrainingRegistration { Id = 1, TrainingtopicId = 1, UserId = "u1", Prefix = "นาย", FirstName = "สมชาย", LastName = "ใจดี" },
            new TrainingRegistration { Id = 2, TrainingtopicId = 1, UserId = "u2", Prefix = "นางสาว", FirstName = "สมหญิง", LastName = "รักเรียน" },
            new TrainingRegistration { Id = 3, TrainingtopicId = 1, UserId = "u3", FirstName = "ยกเลิก", LastName = "แล้ว", Status = "ยกเลิกแล้ว" },
            new TrainingRegistration { Id = 4, TrainingtopicId = 2, UserId = "u1", FirstName = "ไม่มี", LastName = "ปี" });
        _context.SaveChanges();
    }

    private static int PageCount(byte[] pdf) =>
        System.Text.RegularExpressions.Regex.Matches(System.Text.Encoding.Latin1.GetString(pdf), @"/Type\s*/Page[^s]").Count;

    [Fact]
    public async Task Owner_CanDownload_OwnCertificate()
    {
        var pdf = await _export.GenerateTrainingCertificateAsync(1, "u1");
        Assert.Equal("%PDF-", System.Text.Encoding.ASCII.GetString(pdf, 0, 5));
        Assert.Equal(1, PageCount(pdf));
        File.WriteAllBytes(Path.Combine(Path.GetTempPath(), "training_certificate.pdf"), pdf);
    }

    [Fact]
    public async Task Staff_CanDownload_AnyCertificate() =>
        Assert.True((await _export.GenerateTrainingCertificateAsync(2, null)).Length > 1000);

    [Fact]
    public Task OtherUser_IsRejected() =>
        Assert.ThrowsAsync<UnauthorizedAccessException>(() => _export.GenerateTrainingCertificateAsync(1, "u2"));

    [Fact]
    public Task Cancelled_IsRejected() =>
        Assert.ThrowsAsync<InvalidOperationException>(() => _export.GenerateTrainingCertificateAsync(3, "u3"));

    [Fact]
    public Task TopicWithoutYear_IsRejected() =>
        Assert.ThrowsAsync<InvalidOperationException>(() => _export.GenerateTrainingCertificateAsync(4, "u1"));

    [Fact]
    public async Task Topic_AllCertificates_OnePagePerActivePerson()
    {
        var pdf = await _export.GenerateTrainingCertificatesForTopicAsync(1);
        Assert.Equal(2, PageCount(pdf));
    }

    public void Dispose() => _context.Dispose();
}
