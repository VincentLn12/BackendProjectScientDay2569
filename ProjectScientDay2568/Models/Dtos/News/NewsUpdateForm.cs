namespace ProjectScientDay2568.Models.Dtos.News
{
    public class NewsUpdateForm
    {
        public string Title { get; set; } = null!;
        public string? Content { get; set; }
        public DateTime DatePublished { get; set; }

        public IFormFile? NewCoverImage { get; set; }
        public List<IFormFile>? NewContentImages { get; set; }
        public List<string>? RemoveContentImagePaths { get; set; }
        public bool ReplaceContentImages { get; set; }
    }
}
