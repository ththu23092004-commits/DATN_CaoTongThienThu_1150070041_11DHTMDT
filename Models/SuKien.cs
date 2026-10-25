using System;

namespace VeSuKienWeb.Models
{
    public class SuKien
    {
        public int SuKienId { get; set; }
        public string TenSuKien { get; set; } = string.Empty;
        public string? MoTaChiTiet { get; set; }
        public DateTime ThoiGianBatDau { get; set; }
        public DateTime ThoiGianKetThuc { get; set; }

        public int LoaiId { get; set; }
        public int ToChucId { get; set; }

        public string? TrangThaiDuyet { get; set; }   // ChoDuyet / DaDuyet / TuChoi
        public DateTime NgayDang { get; set; } = DateTime.Now;

        public int? SoLuongToiDa { get; set; }
        public decimal? GiaVeMacDinh { get; set; }

        public LoaiSuKien? LoaiSuKien { get; set; }
        public ToChucSuKien? ToChucSuKien { get; set; }

         public string? AnhSuKien { get; set; }


public DiaDiem? DiaDiem { get; set; }

    }
}

