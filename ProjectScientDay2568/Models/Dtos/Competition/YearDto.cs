namespace ProjectScientDay2568.Models.Dtos.Competition
{
    public class YearDto
    {
        public int YearID { get; set; }
        public int YearNumber { get; set; }
        public string YearName { get; set; } = string.Empty;
        public bool isActive { get; set; } = false;
        public string CompetitionRegulationsUrl { get; set; } = string.Empty;
        public string? DateName { get; set; } = string.Empty;
        public DateTime? RegistrationOpenDate { get; set; }
        public DateTime? RegistrationCloseDate { get; set; }
        public int? MinCertificateNumber { get; set; }
        public int? MaxCertificateNumber { get; set; }

    }
}
