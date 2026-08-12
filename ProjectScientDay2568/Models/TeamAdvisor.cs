using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static ProjectScientDay2568.Utility.SD;

namespace ProjectScientDay2568.Models
{
    public class TeamAdvisor
    {
        [Key] public int TeamAdvisorID { get; set; }
        [ForeignKey(nameof(Team))] public int TeamID { get; set; }
        public Team? Team { get; set; }
        public int SortOrder { get; set; }
        public TitlePrefix TitlePrefix { get; set; } = TitlePrefix.นาย;
        [MaxLength(100)] public string? OtherTitlePrefix { get; set; }
        [Required, MaxLength(100)] public string FirstName { get; set; } = string.Empty;
        [Required, MaxLength(100)] public string LastName { get; set; } = string.Empty;
    }
}
