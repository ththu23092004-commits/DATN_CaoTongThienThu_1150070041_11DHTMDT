
using System;

namespace VeSuKienWeb.Models.KhachHang
{
    public class VeCuaToiItemVM
    {
        public int VeId { get; set; }
        public int SuKienId { get; set; }
        public string MaVe { get; set; } = string.Empty;

        public string TenSuKien { get; set; } = string.Empty;
        public string TenLoaiSuKien { get; set; } = string.Empty;

        public string? AnhSuKien { get; set; }

        public DateTime ThoiGianBatDau { get; set; }
        public DateTime ThoiGianKetThuc { get; set; }

        public string TenLoaiVe { get; set; } = string.Empty;
        public decimal GiaVe { get; set; }

        public string? Hang { get; set; }
        public int? Cot { get; set; }

        public string TrangThaiVe { get; set; } = string.Empty;
        public DateTime NgayDat { get; set; }

        public bool LaTrucTiep { get; set; }
        public bool LaTrucTuyen { get; set; }

        public string? DiaDiem { get; set; }
        public string? DiaChi { get; set; }

        public string? OnlineNenTang { get; set; }
        public string? OnlineLinkXem { get; set; }
        public string? OnlineMatKhau { get; set; }

        public bool SuKienDaKetThuc { get; set; }
        public bool CoTheHuy { get; set; }

        public string? DanhSachGhe { get; set; }

        public bool DangChoHoanVe { get; set; }
        public bool DuocXemQr { get; set; }
    }
}
