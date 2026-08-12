namespace ProjectScientDay2568.Models.Dtos
{
    public class NewsDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Content { get; set; }
        public DateTime DatePublished { get; set; }

        public string? CoverImagePath { get; set; }           
        public List<string>? ContentImagePaths { get; set; }   
    }
}
