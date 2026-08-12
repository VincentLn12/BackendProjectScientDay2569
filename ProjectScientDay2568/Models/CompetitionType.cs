namespace ProjectScientDay2568.Models
{
    public class CompetitionType
    {
        [Key]
        public int CompetitionTypeID { get; set; }

        [Required]
        [MaxLength(255)]
        public string CompetitionTypeName { get; set; } = string.Empty;
        public ICollection<Competition>? Competitions { get; set; }

    }
}
