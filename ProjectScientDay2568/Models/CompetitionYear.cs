using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectScientDay2568.Models
{
    public class CompetitionYear
    {
        [Key]
        public int CompetitionYearID { get; set; }

        [ForeignKey("Year")]
        public int YearID { get; set; }
        public Year? Year { get; set; }

        [ForeignKey("Competition")]
        public int CompetitionID { get; set; }
        public Competition? Competition { get; set; }

        public DateTime? CompetitionEndDate { get; set; }
        public string? RegistrationLocation { get; set; }
        public bool IsActive { get; set; } = true; 
        public bool IsRegistrationOpen { get; set; } = false;
        public int AdvisorCount { get; set; } = 1;
        [MaxLength(10)] public string RegistrationMode { get; set; } = "team";
        public int? MaxStudentsPerSchool { get; set; }
    }
}
