
using System;
using System.Collections.Generic;

namespace VeSuKienWeb.Models.BanToChuc
{
    public class ThongKeTongQuanDashboardVM
    {
        public int TongSuKien { get; set; }
        public int TongVeDaBan { get; set; }
        public int TongCheckIn { get; set; }
        public int TongVeDaHuy { get; set; }
        public int YeuCauChoDuyet { get; set; }

        // Giữ tương thích các thuộc tính tài chính đã có.
        public decimal TongDoanhThu { get; set; }
        public decimal DoanhThuGop { get; set; }
        public decimal TongTienHoan { get; set; }

        public int? SuKienDangChon { get; set; }

        public List<SuKienLocThongKeVM> DanhSachSuKien
            { get; set; } = new();

        // Dữ liệu mới dành riêng cho dashboard vận hành.
        public List<HoatDongTheoThangVM> HoatDongTheoThang
            { get; set; } = new();

        public List<TrangThaiSuKienTongQuanVM> TrangThaiSuKien
            { get; set; } = new();

        public List<SuKienSapDienRaVM> SuKienSapDienRa
            { get; set; } = new();

        // Giữ các model cũ để tương thích.
        public List<DoanhThuTheoThangVM> DoanhThuTheoThang
            { get; set; } = new();

        public List<VeBanTheoSuKienVM> VeBanTheoSuKien
            { get; set; } = new();

        public List<TrangThaiVeVM> TrangThaiVe
            { get; set; } = new();

        public List<TrangThaiHoanHuyVM> TrangThaiHoanHuy
            { get; set; } = new();

        public List<SuKienBanChayVM> TopSuKienBanChay
            { get; set; } = new();

        public List<YeuCauHoanHuyGanDayVM> YeuCauGanDay
            { get; set; } = new();
    }

    public class SuKienLocThongKeVM
    {
        public int SuKienId { get; set; }
        public string TenSuKien { get; set; } = "";
    }

    public class HoatDongTheoThangVM
    {
        public string Thang { get; set; } = "";
        public int VeThanhToan { get; set; }
        public int LuotCheckIn { get; set; }
    }

    public class TrangThaiSuKienTongQuanVM
    {
        public string TrangThai { get; set; } = "";
        public int SoLuong { get; set; }
    }

    public class SuKienSapDienRaVM
    {
        public int SuKienId { get; set; }
        public string TenSuKien { get; set; } = "";
        public DateTime ThoiGianBatDau { get; set; }
        public DateTime ThoiGianKetThuc { get; set; }
        public int SoVeHieuLuc { get; set; }
    }

    public class DoanhThuTheoThangVM
    {
        public string Thang { get; set; } = "";
        public decimal DoanhThu { get; set; }
        public decimal DoanhThuGop { get; set; }
        public decimal TienHoan { get; set; }
    }

    public class VeBanTheoSuKienVM
    {
        public int SuKienId { get; set; }
        public string TenSuKien { get; set; } = "";
        public int SoVe { get; set; }
    }

    public class TrangThaiVeVM
    {
        public string TrangThai { get; set; } = "";
        public int SoLuong { get; set; }
    }

    public class TrangThaiHoanHuyVM
    {
        public string TrangThai { get; set; } = "";
        public int SoLuong { get; set; }
    }

    public class SuKienBanChayVM
    {
        public int SuKienId { get; set; }
        public string TenSuKien { get; set; } = "";
        public int SoVeDaBan { get; set; }
        public int SoVeDaCheckIn { get; set; }
        public decimal DoanhThu { get; set; }
        public decimal DoanhThuGop { get; set; }
        public decimal TienHoan { get; set; }
    }

    public class YeuCauHoanHuyGanDayVM
    {
        public string MaVe { get; set; } = "";
        public string TenSuKien { get; set; } = "";
        public string LyDo { get; set; } = "";
        public string TrangThai { get; set; } = "";
        public DateTime NgayYeuCau { get; set; }
    }
}
