using System;
using System.Collections.Generic;
using VeSuKienWeb.Models;

namespace VeSuKienWeb.Models.KhachHang
{
    // ========= ITEM CHO LIST SỰ KIỆN =========
    public class SuKienListItemKhVM
    {
        public int SuKienId { get; set; }
        public string TenSuKien { get; set; } = string.Empty;
        public string? TenLoai { get; set; }
        public DateTime ThoiGianBatDau { get; set; }
        public DateTime ThoiGianKetThuc { get; set; }
        public string? DiaDiem { get; set; }    // nếu là offline
        public decimal? GiaTu { get; set; }     // giá vé thấp nhất
        public bool DaKetThuc { get; set; }

        // ⭐ THUỘC TÍNH HÌNH ẢNH SỰ KIỆN
        public string? AnhSuKien { get; set; }
    }

    // ========= CHI TIẾT SỰ KIỆN CHO KHÁCH =========
 public class SuKienChiTietKhVM
{
    public SuKien SuKien { get; set; } = null!;
    public LoaiSuKien? LoaiSuKien { get; set; }
    public ToChucSuKien? ToChuc { get; set; }
    public DiaDiem? DiaDiem { get; set; }
    public SuKienTrucTuyen? SuKienTrucTuyen { get; set; }

    public List<LoaiVe> LoaiVes { get; set; } = new();

    public List<DatVeGheItemVM> GheList { get; set; } = new();

    public bool CoTheDatVe { get; set; }

    public double? DiemTrungBinh { get; set; }
    public int TongSoDanhGia { get; set; }

    
}

    // ========= THÔNG TIN 1 GHẾ =========
    public class DatVeGheItemVM
    {
        public int ChoNgoiId { get; set; }
        public string Hang { get; set; } = string.Empty;
        public int Cot { get; set; }
        public string TrangThai { get; set; } = "Trong";
        public string TenLoaiVe { get; set; } = string.Empty;
        public decimal GiaVe { get; set; }
    }

    // ========= MODEL ĐẶT VÉ =========
    public class DatVeKhVM
    {
        public int SuKienId { get; set; }
        public string TenSuKien { get; set; } = string.Empty;
        public bool LaSuKienTrucTiep { get; set; }
        public bool LaWorkshop { get; set; }

        // Online
        public int? LoaiVeId { get; set; }
        public int SoLuong { get; set; } = 1;

        // Offline (chọn ghế)
        public List<DatVeGheItemVM> GheList { get; set; } = new();
        public List<int> GheChonIds { get; set; } = new();
    }

    // ========= MODEL THANH TOÁN =========
    public class VeThanhToanItemVM
    {
        public int VeId { get; set; }
        public string MaVe { get; set; } = string.Empty;
        public string TenSuKien { get; set; } = string.Empty;
        public string TenLoaiVe { get; set; } = string.Empty;
        public string? Hang { get; set; }
        public int? Cot { get; set; }
        public decimal GiaVe { get; set; }
    }

    public class ThanhToanKhVM
    {
        public int DonHangId { get; set; }
        public decimal TongTien { get; set; }
        public List<VeThanhToanItemVM> Ves { get; set; } = new();
    }
}
