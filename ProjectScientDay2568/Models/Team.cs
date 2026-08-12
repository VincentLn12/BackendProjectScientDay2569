using System.ComponentModel.DataAnnotations.Schema;
using ProjectScientDay2568.Models.YourNamespace.Models;
using static ProjectScientDay2568.Utility.SD;

namespace ProjectScientDay2568.Models
{
    public class Team
    {
        // 🔹 Primary Key
        [Key]
        public int TeamID { get; set; }

        // 🔹 Identification
        public int? NameOrdinal { get; set; } = 0; // ใช้สำหรับถ้าชื่อซ้ำ ให้รัน 1 2 3
        public DateTime RegistrationDateTime { get; set; } = DateTime.Now;

        // 🔹 Relationships: Competition
        [ForeignKey("CompetitionLevel")]
        public int? CompetitionLevelID { get; set; }
        public CompetitionLevel? CompetitionLevel { get; set; }

        // 🔹 Relationships: School
        public int? school_code { get; set; }
        [ForeignKey("school_code")]
        public Schools schools { get; set; }

        // 🔹 Relationships: Reward Type
        public int? RewardTypeID { get; set; }
        [ForeignKey("RewardTypeID")]
        public RewardType RewardType { get; set; }

        // 🔹 ผู้จัดการทีมหลัก
        public TitlePrefix PrimaryTitlePrefix { get; set; } = TitlePrefix.นาย;
        [MaxLength(100)] public string? PrimaryOtherTitlePrefix { get; set; }

        [MaxLength(100)]
        public string? PrimaryFirstName { get; set; }

        [MaxLength(100)]
        public string? PrimaryLastName { get; set; }

        // 🔹 ผู้จัดการทีมรอง
        public TitlePrefix SecondaryTitlePrefix { get; set; } = TitlePrefix.นาย;
        [MaxLength(100)] public string? SecondaryOtherTitlePrefix { get; set; }

        [MaxLength(100)]
        public string? SecondaryFirstName { get; set; }

        [MaxLength(100)]
        public string? SecondaryLastName { get; set; }

        public TitlePrefix ThirdTitlePrefix { get; set; } = TitlePrefix.นาย;
        [MaxLength(100)] public string? ThirdOtherTitlePrefix { get; set; }
        [MaxLength(100)] public string? ThirdFirstName { get; set; }
        [MaxLength(100)] public string? ThirdLastName { get; set; }
        public TitlePrefix FourthTitlePrefix { get; set; } = TitlePrefix.นาย;
        [MaxLength(100)] public string? FourthOtherTitlePrefix { get; set; }
        [MaxLength(100)] public string? FourthFirstName { get; set; }
        [MaxLength(100)] public string? FourthLastName { get; set; }
        public TitlePrefix FifthTitlePrefix { get; set; } = TitlePrefix.นาย;
        [MaxLength(100)] public string? FifthOtherTitlePrefix { get; set; }
        [MaxLength(100)] public string? FifthFirstName { get; set; }
        [MaxLength(100)] public string? FifthLastName { get; set; }

        // 🔹 ข้อมูลโครงการ
        [MaxLength(255)]
        public string? ProjectPDFLink { get; set; } // path ไฟล์โครงการ PDF
        public string? ProjectName { get; set; } = string.Empty; // ชื่อโครงการ

        // 🔹 สถานะทีม
        public TeamStatus Status { get; set; }  // สถานะของทีม
        public DateTime? StatusTime { get; set; } = null; // เวลาที่เปลี่ยนสถานะ

        // 🔹 ข้อมูลอื่น ๆ
        public bool IsLuckyDrawWinner { get; set; } = false; // ชนะรางวัล Lucky Draw หรือไม่
        public int? TeamNumberInLevel { get; set; } = 0; // หมายเลขทีมในระดับการแข่งขัน

        // 🔹 Relationships: User
        [ForeignKey("User")]
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        // 🔹 Collections
        public ICollection<Student>? Students { get; set; }
        public ICollection<TeamCertificate>? TeamCertificates { get; set; }
        public ICollection<TeamProject> TeamProjects { get; set; } = new List<TeamProject>();
        public ICollection<TeamAdvisor> Advisors { get; set; } = new List<TeamAdvisor>();
    }

}
