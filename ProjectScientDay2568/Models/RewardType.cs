namespace ProjectScientDay2568.Models
{
    public class RewardType
    {
        [Key]
        public int RewardTypeID { get; set; }

        [Required]
        [MaxLength(50)]
        public string RewardTypeName { get; set; } = string.Empty;

        // Navigation properties
    }
}
