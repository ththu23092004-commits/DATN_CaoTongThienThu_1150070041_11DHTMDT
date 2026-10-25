
using System;
using System.Collections.Generic;

namespace VeSuKienWeb.Models.BanToChuc
{
    public class ThongKeTongQuanDashboardVM
    {
        public int TongSuKien { get; set; }
        public int TongVeDaBan { get; set; }
        public decimal TongDoanhThu { get; set; }
        public int YeuCauChoDuyet { get; set; }

        public List<DoanhThuTheoThangVM> DoanhThuTheoThang { get; set; } = new();
        public List<VeBanTheoSuKienVM> VeBanTheoSuKien { get; set; } = new();
        public List<TrangThaiVeVM> TrangThaiVe { get; set; } = new();
        public List<TrangThaiHoanHuyVM> TrangThaiHoanHuy { get; set; } = new();

        public List<SuKienBanChayVM> TopSuKienBanChay { get; set; } = new();
        public List<YeuCauHoanHuyGanDayVM> YeuCauGanDay { get; set; } = new();
    }

    public class DoanhThuTheoThangVM
    {
        public string Thang { get; set; } = string.Empty;
        public decimal DoanhThu { get; set; }
    }

    public class VeBanTheoSuKienVM
    {
        public string TenSuKien { get; set; } = string.Empty;
        public int SoVe { get; set; }
    }

    public class TrangThaiVeVM
    {
        public string TrangThai { get; set; } = string.Empty;
        public int SoLuong { get; set; }
    }

    public class TrangThaiHoanHuyVM
    {
        public string TrangThai { get; set; } = string.Empty;
        public int SoLuong { get; set; }
    }

    public class SuKienBanChayVM
    {
        public string TenSuKien { get; set; } = string.Empty;
        public int SoVeDaBan { get; set; }
        public decimal DoanhThu { get; set; }
    }

    public class YeuCauHoanHuyGanDayVM
    {
        public string MaVe { get; set; } = string.Empty;
        public string TenSuKien { get; set; } = string.Empty;
        public string LyDo { get; set; } = string.Empty;
        public string TrangThai { get; set; } = string.Empty;
        public DateTime NgayYeuCau { get; set; }
    }
}