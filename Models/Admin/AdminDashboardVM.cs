
using System;
using System.Collections.Generic;

namespace VeSuKienWeb.Models.Admin
{
    public class AdminDashboardVM
    {
        // ============================
        // BO LOC THOI GIAN
        // ============================

        public string KyThongKe { get; set; } = "180";
        public DateTime TuNgay { get; set; }
        public DateTime DenNgay { get; set; }

        public DateTime CapNhatLuc { get; set; } = DateTime.Now;

        // ============================
        // OVERVIEW - TOAN HE THONG
        // ============================

        public int TongNguoiDung { get; set; }
        public int TongBanToChuc { get; set; }
        public int TongSuKien { get; set; }

        public int SuKienChoDuyet { get; set; }
        public int SuKienDaDuyet { get; set; }
        public int SuKienTuChoi { get; set; }
        public int SuKienDaAn { get; set; }

        public int TaiKhoanBiKhoa { get; set; }
        public int YeuCauHuyChoXuLy { get; set; }

        public int TongDonThanhCong { get; set; }
        public int TongVeBan { get; set; }

        public decimal TongDoanhThu { get; set; }
        public decimal TongTienHoanGhiNhan { get; set; }

        // ============================
        // OVERVIEW - TRONG KY
        // ============================

        public int NguoiDungMoiTrongKy { get; set; }
        public int BanToChucMoiTrongKy { get; set; }
        public int SuKienMoiTrongKy { get; set; }

        public int DonThanhCongTrongKy { get; set; }
        public int VeBanTrongKy { get; set; }

        public decimal DoanhThuTrongKy { get; set; }

        // ============================
        // BREAKDOWN - LOAI SU KIEN
        // ============================

        public List<string> TenLoaiSuKien { get; set; } = new();
        public List<int> SoLuongTheoLoai { get; set; } = new();

        // ============================
        // BREAKDOWN - HINH THUC
        // ============================

        public int SuKienTrucTiep { get; set; }
        public int SuKienTrucTuyen { get; set; }
        public int SuKienHybrid { get; set; }

        // ============================
        // BREAKDOWN - XU HUONG
        // ============================

        public List<string> ThangSuKien { get; set; } = new();
        public List<int> SoSuKienTheoThang { get; set; } = new();

        public List<string> ThangNguoiDung { get; set; } = new();
        public List<int> SoNguoiDungTheoThang { get; set; } = new();

        public List<string> ThangDoanhThu { get; set; } = new();
        public List<decimal> DoanhThuTheoThang { get; set; } = new();
        public List<int> VeBanTheoThang { get; set; } = new();

        // ============================
        // BREAKDOWN - TOP SU KIEN
        // ============================

        public List<AdminTopSuKienVM> TopSuKien { get; set; } = new();
    }

    public class AdminTopSuKienVM
    {
        public int SuKienId { get; set; }
        public string TenSuKien { get; set; } = string.Empty;

        public int SoVeBan { get; set; }
        public decimal DoanhThuPhanBo { get; set; }
    }
}
