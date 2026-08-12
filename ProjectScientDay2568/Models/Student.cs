using System.ComponentModel.DataAnnotations.Schema;
using static ProjectScientDay2568.Utility.SD;

namespace ProjectScientDay2568.Models
{
    public class Student
    {
        [Key]
        public int StudentID { get; set; }

        public TitlePrefix TitlePrefix { get; set; } = TitlePrefix.นาย;

        [Required]
        [MaxLength(255)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string LastName { get; set; } = string.Empty;

        [ForeignKey("Team")]
        public int TeamID { get; set; }
        public Team? Team { get; set; }

        public bool IsMainMember { get; set; } = true;
        public int? RewardTypeID { get; set; }
        [ForeignKey("RewardTypeID")]
        public RewardType? RewardType { get; set; }
        public bool IsCertificateGenerated { get; set; } = false;
        public bool IsCheckedIn { get; set; } = false;
    }
}
