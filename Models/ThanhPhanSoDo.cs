
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VeSuKienWeb.Models
{
    public class ThanhPhanSoDo
    {
        [Key]
        public int ThanhPhanId { get; set; }

        public int SoDoId { get; set; }

        [Required]
        [MaxLength(30)]
        public string LoaiThanhPhan { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? TenHienThi { get; set; }

        [Column(TypeName = "decimal(9,2)")]
        public decimal ViTriX { get; set; }

        [Column(TypeName = "decimal(9,2)")]
        public decimal ViTriY { get; set; }

        [Column(TypeName = "decimal(9,2)")]
        public decimal ChieuRong { get; set; }

        [Column(TypeName = "decimal(9,2)")]
        public decimal ChieuCao { get; set; }

        [Column(TypeName = "decimal(9,2)")]
        public decimal GocXoay { get; set; }

        public SoDoSuKien SoDoSuKien { get; set; } = null!;
    }
}

