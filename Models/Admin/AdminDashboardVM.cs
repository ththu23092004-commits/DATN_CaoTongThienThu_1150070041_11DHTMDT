namespace VeSuKienWeb.Models.Admin
{
    public class AdminDashboardVM
    {
        public int TongNguoiDung { get; set; }
        public int TongBanToChuc { get; set; }
        public int TongSuKien { get; set; }
        public int SuKienChoDuyet { get; set; }
        public int SuKienDaDuyet { get; set; }
        public int SuKienTuChoi { get; set; }

        public List<string> TenLoaiSuKien { get; set; } = new();
        public List<int> SoLuongTheoLoai { get; set; } = new();

        public int SuKienTrucTiep { get; set; }
        public int SuKienTrucTuyen { get; set; }
        public int SuKienHybrid { get; set; }

        public List<string> ThangSuKien { get; set; } = new();
        public List<int> SoSuKienTheoThang { get; set; } = new();

        public List<string> ThangNguoiDung { get; set; } = new();
        public List<int> SoNguoiDungTheoThang { get; set; } = new();
    }
}