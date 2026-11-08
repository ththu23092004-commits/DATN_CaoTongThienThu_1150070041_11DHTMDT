
using System;
using System.Collections.Generic;

namespace VeSuKienWeb.Models.BanToChuc
{
    public class QuanLyVeItemBtcVM
    {
        public int LoaiVeId { get; set; }
        public string TenLoai { get; set; } = string.Empty;
        public decimal GiaVe { get; set; }
        public int SoLuongConfig { get; set; }
        public int SoLuongDaBan { get; set; }
        public string? MoTa { get; set; }
    }

    public class QuanLyVeBtcVM
    {
        public int SuKienId { get; set; }
        public string TenSuKien { get; set; } = string.Empty;
        public List<QuanLyVeItemBtcVM> LoaiVes { get; set; } = new();
    }

    public class ThemLoaiVeBtcVM
    {
        public int SuKienId { get; set; }
        public string TenLoai { get; set; } = string.Empty;
        public decimal GiaVe { get; set; }
        public int SoLuong { get; set; }
        public string? MoTa { get; set; }
    }

    public class CapNhatLoaiVeBtcVM
    {
        public int LoaiVeId { get; set; }
        public int SuKienId { get; set; }
        public string TenLoai { get; set; } = string.Empty;
        public decimal GiaVe { get; set; }
        public int SoLuong { get; set; }
        public string? MoTa { get; set; }
    }

    public class DanhSachVeThamGiaItemBtcVM
    {
        public int VeId { get; set; }
        public string MaVe { get; set; } = string.Empty;
        public string HoTenKhach { get; set; } = string.Empty;
        public string EmailKhach { get; set; } = string.Empty;
        public string TenLoaiVe { get; set; } = string.Empty;
        public string TrangThaiVe { get; set; } = string.Empty;
        public bool DaCheckIn { get; set; }
        public DateTime? ThoiGianCheckIn { get; set; }
    }

    public class DanhSachVeThamGiaBtcVM
    {
        public int SuKienId { get; set; }
        public string TenSuKien { get; set; } = string.Empty;
        public List<DanhSachVeThamGiaItemBtcVM> DanhSach { get; set; } = new();
    }

    public class YeuCauHoanHuyItemBtcVM
    {
        public int YeuCauId { get; set; }
        public string MaVe { get; set; } = string.Empty;
        public string TenSuKien { get; set; } = string.Empty;
        public string HoTenKhach { get; set; } = string.Empty;
        public string TrangThai { get; set; } = string.Empty;
        public string HinhThuc { get; set; } = string.Empty;
        public string LyDo { get; set; } = string.Empty;
        public DateTime NgayYeuCau { get; set; }
        
public List<string> DanhSachGhe { get; set; } = new();

    }

    public class GuiThongBaoBtcVM
    {
        public int SuKienId { get; set; }
        public string NoiDung { get; set; } = string.Empty;
    }
}
