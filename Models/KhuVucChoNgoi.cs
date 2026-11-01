
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VeSuKienWeb.Models
{
    public class KhuVucChoNgoi
    {
        [Key]
        public int KhuVucId { get; set; }

        public int SoDoId { get; set; }

        [Required]
        [MaxLength(100)]
        public string TenKhu { get; set; } = string.Empty;

        [MaxLength(7)]
        public string MauSac { get; set; } = "#4F83CC";

        public int? LoaiVeId { get; set; }

        public int ThuTu { get; set; }

        public SoDoSuKien SoDoSuKien { get; set; } = null!;

        public LoaiVe? LoaiVe { get; set; }

        public ICollection<SoDoChoNgoi> GheNgois { get; set; }
            = new List<SoDoChoNgoi>();
    }
}

