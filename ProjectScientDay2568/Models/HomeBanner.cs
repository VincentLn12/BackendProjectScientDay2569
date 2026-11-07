using System.ComponentModel.DataAnnotations;

namespace ProjectScientDay2568.Models
{
    public class HomeBanner
    {
        [Key] public int Id { get; set; }
        [Required, MaxLength(500)] public string ImagePath { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
        // แสดงเป็น Popup ตอนเข้าเว็บ (แยกจากสไลด์หน้าแรก)
        public bool ShowInPopup { get; set; }
    }
}
