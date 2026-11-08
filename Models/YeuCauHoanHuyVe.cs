using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VeSuKienWeb.Models
{
    [Table("YeuCauHoanHuyVe")]
    public class YeuCauHoanHuyVe
    {
        [Key] // 🔑 Khai báo rõ ràng khóa chính
        public int YeuCauId { get; set; }

        [Required]
        public int VeId { get; set; }

        [Required]
        [MaxLength(300)]
        public string LyDo { get; set; } = string.Empty;

        public DateTime NgayYeuCau { get; set; } = DateTime.Now;

        // ChoDuyet, DaDuyet, TuChoi
        [Required]
        [MaxLength(20)]
        public string TrangThai { get; set; } = "ChoDuyet";

        // HuyVe (giữ cột để tương thích dữ liệu cũ)
        [Required]
        [MaxLength(20)]
        public string HinhThuc { get; set; } = "HuyVe";

        // Navigation
        public Ve Ve { get; set; } = null!;
    }
}
