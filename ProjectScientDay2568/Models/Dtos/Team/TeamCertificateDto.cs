namespace ProjectScientDay2568.Models.Dtos.Team
{
    public class TeamCertificateDto
    {
        public int TeamID { get; set; }
        public string TeamName { get; set; }  // เพิ่มชื่อทีม
        public string CertificateNumber { get; set; }
        public string? CertificateName { get; set; } // เช่น กจ.331/2568

        public DateTime IssuedDate { get; set; }
    }
}
