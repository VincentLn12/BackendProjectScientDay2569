namespace ProjectScientDay2568.Models.Dtos.Team
{
    public class UpdateCertificateConfigRequest
    {
        public int StartNumber { get; set; }
        public string? CertificateName { get; set; }
        public string? UniversityDeanTitle { get; set; }
        public string? UniversityDeanName { get; set; }
        public string? FacultyDeanTitle { get; set; }
        public string? FacultyDeanName { get; set; }
    }
}
