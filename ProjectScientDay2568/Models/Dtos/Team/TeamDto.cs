using ProjectScientDay2568.Utility;
using static ProjectScientDay2568.Utility.SD;

namespace ProjectScientDay2568.Models.Dtos.Team
{
    public class TeamDto
    {
        public int TeamID { get; set; }
        public int? NameOrdinal { get; set; }
        public int? School_code { get; set; } 
        public string? school_name { get; set; } = string.Empty;
        public string? province { get; set; }
        public string? district { get; set; }


        public int PrimaryTitlePrefix { get; set; } 
        public string? PrimaryOtherTitlePrefix { get; set; }
        public string? PrimaryFirstName { get; set; }
        public string? PrimaryLastName { get; set; }
        public int SecondaryTitlePrefix { get; set; } 
        public string? SecondaryOtherTitlePrefix { get; set; }
        public string? SecondaryFirstName { get; set; }
        public string? SecondaryLastName { get; set; }
        public int ThirdTitlePrefix { get; set; }
        public string? ThirdOtherTitlePrefix { get; set; }
        public string? ThirdFirstName { get; set; }
        public string? ThirdLastName { get; set; }
        public int FourthTitlePrefix { get; set; }
        public string? FourthOtherTitlePrefix { get; set; }
        public string? FourthFirstName { get; set; }
        public string? FourthLastName { get; set; }
        public int FifthTitlePrefix { get; set; }
        public string? FifthOtherTitlePrefix { get; set; }
        public string? FifthFirstName { get; set; }
        public string? FifthLastName { get; set; }

        public string? UserId { get; set; }
        public TeamStatus Status { get; set; } 
        public DateTime RegistrationDateTime { get; set; } = DateTime.Now;
        public DateTime? StatusTime { get; set; } = null;

        public string? LevelName { get; set; } 
        public string? CompetitionName { get; set; }
        public string? ProjectPDFLink { get; set; } // ลิงก์ PDF ที่อัปโหลดแล้ว
        public string? ProjectName { get; set; } // ชื่อโครงงาน
        public string? RewardType { get; set; }
        public int? RewardTypeID { get; set; }
        public int? CompetitionLevelID { get; set; }
        public int? TeamNumberInLevel { get; set; } = 0; 
        public bool HasCertificate { get; set; }

        public List<StudentDto> Students { get; set; } = new();
        // ✅ ชื่อสมาชิกแบบข้อความก้อนเดียว (ไว้แสดงในตาราง)
        public string StudentNames =>
       (Students == null || Students.Count == 0)
           ? string.Empty
           : string.Join(", ",
               Students.Select(s =>
                   $"{GetTitlePrefixText(s.TitlePrefix)} {(s.FirstName ?? "").Trim()} {(s.LastName ?? "").Trim()}".Trim()
               ).Where(x => !string.IsNullOrWhiteSpace(x))
           );

        // ถ้าส่ง TitlePrefix เป็นตัวเลข
        private static string GetTitlePrefixText(int titlePrefix) => ((SD.TitlePrefix)titlePrefix) switch
        {
            SD.TitlePrefix.นาย => "นาย",
            SD.TitlePrefix.นาง => "นาง",
            SD.TitlePrefix.นางสาว => "นางสาว",
            SD.TitlePrefix.เด็กชาย => "เด็กชาย",
            SD.TitlePrefix.เด็กหญิง => "เด็กหญิง",
            SD.TitlePrefix.ว่าที่ร้อยตรี => "ว่าที่ร้อยตรี",
            SD.TitlePrefix.ว่าที่ร้อยตรีหญิง => "ว่าที่ร้อยตรีหญิง",
            SD.TitlePrefix.Other => "อื่น ๆ",
            _ => string.Empty
        };

    }
}
