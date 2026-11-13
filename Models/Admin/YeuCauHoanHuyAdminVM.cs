
using System;

namespace VeSuKienWeb.Models.Admin
{
    public class YeuCauHoanHuyAdminVM
    {
        public int YeuCauId { get; set; }
        public int VeId { get; set; }

        public string MaVe { get; set; } = string.Empty;
        public string TenSuKien { get; set; } = string.Empty;

        // Ban tổ chức phụ trách sự kiện.
        public string BanToChucId { get; set; } = string.Empty;
        public string TenBanToChuc { get; set; } = string.Empty;

        // Khách hàng gửi yêu cầu.
        public string TenNguoiDung { get; set; } = string.Empty;
        public string EmailNguoiDung { get; set; } = string.Empty;

        public decimal SoTien { get; set; }
        public DateTime NgayYeuCau { get; set; }

        public string LyDo { get; set; } = string.Empty;
        public string HinhThuc { get; set; } = string.Empty;

        // ChoDuyet / DaHuy / TuChoi
        public string TrangThai { get; set; } = string.Empty;
    }
}
