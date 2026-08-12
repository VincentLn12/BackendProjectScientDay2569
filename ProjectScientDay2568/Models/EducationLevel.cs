namespace ProjectScientDay2568.Models
{
    public class EducationLevel
    {
        [Key]
        public int LevelID { get; set; }

        [Required]
        [MaxLength(50)]
        public string LevelName { get; set; } = string.Empty;
        public ICollection<CompetitionLevel>? CompetitionLevels { get; set; }
    }
}
