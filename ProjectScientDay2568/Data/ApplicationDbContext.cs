using ProjectScientDay2568.Models.YourNamespace.Models;

namespace ProjectScientDay2568.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<Year> Years { get; set; }
        public DbSet<CompetitionType> CompetitionTypes { get; set; }
        public DbSet<Competition> Competitions { get; set; }
        public DbSet<CompetitionYear> CompetitionYears { get; set; }
        public DbSet<EducationLevel> EducationLevels { get; set; }
        public DbSet<CompetitionLevel> CompetitionLevels { get; set; }
        public DbSet<RewardType> RewardTypes { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Match> Matches { get; set; }
        public DbSet<MatchDrawLog> MatchDrawLogs { get; set; }
        public DbSet<TeamCertificate> TeamCertificates { get; set; }
        public DbSet<CertificateConfig> CertificateConfigs { get; set; }
        public DbSet<Schools> School { get; set; }
        public DbSet<News> News { get; set; }
        public DbSet<TeamProject> TeamProjects { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }
        public DbSet<HomeBanner> HomeBanners { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<TeamAdvisor> TeamAdvisors { get; set; }
        public DbSet<Branch> Branches { get; set; }
        public DbSet<Trainingtopic> Trainingtopics { get; set; }
        public DbSet<TrainingRegistration> TrainingRegistrations { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (optionsBuilder.IsConfigured)
            {
                return;
            }

            //base.OnConfiguring(optionsBuilder);
            //optionsBuilder.UseSqlServer("Server=DESKTOP-FUBH5IH\\SQLEXPRESS;Database=ProjectScientDay;TrustServerCertificate=True;Trusted_Connection=True;");

            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseSqlServer("Server=10.103.0.15,1433;Database=ProjectScientDay; Trusted_Connection = false; MultipleActiveResultSets = true; TrustServerCertificate = True; User Id = student;Password = Com@2022; Encrypt = false; ");

        }   

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<Year>()
            .HasOne(y => y.CertificateConfig)
            .WithOne(c => c.Year)
            .HasForeignKey<CertificateConfig>(c => c.YearID);

            builder.Entity<Branch>().HasData(
                new Branch { Id = 1, Name = "สาขาวิชาคอมพิวเตอร์และเทคโนโลยีดิจิทัล" },
                new Branch { Id = 2, Name = "สาขาวิชาฟิสิกส์" },
                new Branch { Id = 3, Name = "สาขาวิชาเคมี" },
                new Branch { Id = 4, Name = "สาขาวิชาชีววิทยา" },
                new Branch { Id = 5, Name = "สาขาวิชาคณิตศาสตร์" },
                new Branch { Id = 6, Name = "สาขาวิชาวิทยาศาสตร์ทั่วไป" }
            );

            base.OnModelCreating(builder);
        }
    }
}