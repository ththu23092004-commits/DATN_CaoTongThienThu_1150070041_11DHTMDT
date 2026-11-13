
using System;
using System.Collections.Generic;

namespace VeSuKienWeb.Models.BanToChuc
{
    public class DoanhThuSuKienItemVM
    {
        public int SuKienId { get; set; }
        public string TenSuKien { get; set; } = "";

        public DateTime ThoiGianBatDau { get; set; }
        public DateTime ThoiGianKetThuc { get; set; }

        public int SoVeDaBan { get; set; }
        public int SoVeDaCheckIn { get; set; }
        public int SoVeDaHuy { get; set; }
        public int TongSoVePhatHanh { get; set; }

        public decimal TongThanhToan { get; set; }
        public decimal TongHoan { get; set; }

        public decimal DoanhThuThucNhan =>
            TongThanhToan - TongHoan;

        public decimal TyLeHoan =>
            TongThanhToan > 0
                ? TongHoan / TongThanhToan * 100m
                : 0m;

        public decimal DoanhThuBinhQuanVe =>
            SoVeDaBan > 0
                ? DoanhThuThucNhan / SoVeDaBan
                : 0m;

        public List<DoanhThuThangSuKienVM> TheoThang
            { get; set; } = new();

        public List<DoanhThuLoaiVeVM> TheoLoaiVe
            { get; set; } = new();
    }

    public class DoanhThuThangSuKienVM
    {
        public string Thang { get; set; } = "";
        public decimal ThanhToan { get; set; }
        public decimal HoanTien { get; set; }

        public decimal DoanhThuThuan =>
            ThanhToan - HoanTien;
    }

    public class DoanhThuLoaiVeVM
    {
        public string TenLoaiVe { get; set; } = "";
        public int SoVeThanhToan { get; set; }
        public decimal DoanhThuGop { get; set; }
    }
}
