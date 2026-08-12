namespace ProjectScientDay2568.Models.Dtos.News
{
    public class NewsCreateForm
    {
        public string Title { get; set; } = null!;
        public string? Content { get; set; }
        public DateTime DatePublished { get; set; }

        public IFormFile? CoverImage { get; set; }
        public List<IFormFile>? ContentImages { get; set; }
    }
}
