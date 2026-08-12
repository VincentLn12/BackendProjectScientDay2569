namespace ProjectScientDay2568.Models
{
    using System.ComponentModel.DataAnnotations;

    namespace YourNamespace.Models
    {
        public class Schools
        {   
            [Key]
            public int school_code { get; set; }    
            [Required]
            [MaxLength(200)]
            public string school_name { get; set; }
            [MaxLength(100)]
            public string? subdistrict { get; set; }
            [MaxLength(100)]
            public string? district { get; set; }
            [MaxLength(100)]
            public string? province { get; set; }
        }
    }

}
