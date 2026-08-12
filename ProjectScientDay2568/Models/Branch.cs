using System.ComponentModel.DataAnnotations;

namespace ProjectScientDay2568.Models
{
    public class Branch
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;
    }
}
