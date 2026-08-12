namespace ProjectScientDay2568.Models
{
    public class News
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Content { get; set; }
        public DateTime DatePublished { get; set; } = DateTime.Now;

        public string? CoverImagePath { get; set; } //หน้าปก
        public List<string>? ContentImagePaths { get; set; } //รูปภาพในเนื้อหา
    }
}
