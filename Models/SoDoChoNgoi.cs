using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VeSuKienWeb.Models
{
    public class SoDoChoNgoi
    {
        [Key]
        public int ChoNgoiId { get; set; }

        [Required]
        [ForeignKey(nameof(DiaDiem))]
        public int DiaDiemId { get; set; }

        [MaxLength(5)]
        [Display(Name = "Hàng")]
        public string? Hang { get; set; }   // A, B, C...

        [Display(Name = "Số ghế")]
        public int Cot { get; set; }        // 1,2,3,...

        [MaxLength(20)]
        [Display(Name = "Trạng thái")]
        public string? TrangThai { get; set; } = "Trong";  // Trống / Đã đặt

        [Required]
        [ForeignKey(nameof(LoaiVe))]
        public int LoaiVeId { get; set; }

        // Navigation
        public DiaDiem DiaDiem { get; set; } = null!;
        public LoaiVe LoaiVe { get; set; } = null!;

        public int? KhuVucId { get; set; }
        // Thông tin giữ ghế khi khách đang thanh toán
public int? DonHangGiuId { get; set; }

public DateTime? ThoiGianHetHanGiu { get; set; }

        [Column(TypeName = "decimal(9,2)")]
        public decimal? ViTriX { get; set; }

        [Column(TypeName = "decimal(9,2)")]
        public decimal? ViTriY { get; set; }

        public KhuVucChoNgoi? KhuVucChoNgoi { get; set; }


        // Nếu sau này bạn có entity Ve thì có thể thêm lại navigation:
        // public ICollection<Ve> DanhSachVe { get; set; } = new List<Ve>();
    }
}
