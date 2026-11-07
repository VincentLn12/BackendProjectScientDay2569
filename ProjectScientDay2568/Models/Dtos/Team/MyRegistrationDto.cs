namespace ProjectScientDay2568.Models.Dtos.Team
{
    /// <summary>
    /// หนึ่งแถวในหน้า "ประวัติการลงทะเบียนกิจกรรม" ของผู้ใช้
    /// รวมข้อมูลที่หน้าเว็บต้องใช้ต่อ (ปี กิจกรรม ระดับชั้น เงื่อนไขสมาชิก) เพื่อไม่ต้องพึ่งข้อมูลจากหน้าก่อนหน้า
    /// </summary>
    public class MyRegistrationDto
    {
        public int TeamID { get; set; }
        public int? NameOrdinal { get; set; }
        public int Status { get; set; }
        public DateTime RegistrationDateTime { get; set; }

        public int? School_code { get; set; }
        public string? school_name { get; set; }
        public string? province { get; set; }
        public string? district { get; set; }

        public string? ProjectName { get; set; }
        public string? ProjectPDFLink { get; set; }
        public bool HasCertificate { get; set; }

        public int MainFilledCount { get; set; }
        public int ReserveFilledCount { get; set; }

        // ระดับชั้น
        public int CompetitionLevelID { get; set; }
        public string? LevelName { get; set; }
        public DateTime? CompetitionDateTime { get; set; }
        public int? MainPlayersCount { get; set; }
        public int? ReservePlayersCount { get; set; }
        public bool IsCertificateNumberGenerated { get; set; }

        // กิจกรรมในปีนั้น
        public int CompetitionYearID { get; set; }
        public string? CompetitionName { get; set; }
        public bool RequiresProject { get; set; }
        public string RegistrationMode { get; set; } = "team";
        public int AdvisorCount { get; set; }
        public int? MaxStudentsPerSchool { get; set; }
        public string? RegistrationLocation { get; set; }

        // ปี
        public int YearID { get; set; }
        public int YearNumber { get; set; }
        public string? YearName { get; set; }
        public DateTime? RegistrationCloseDate { get; set; }
    }
}
