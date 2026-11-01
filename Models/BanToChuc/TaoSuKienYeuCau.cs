using System;
using System.ComponentModel.DataAnnotations;

namespace VeSuKienWeb.Models.BanToChuc
{
    public class TaoSuKienYeuCau
    {
        [Required]
        [Display(Name = "Tên sự kiện")]
        public string TenSuKien { get; set; } = string.Empty;

        [Display(Name = "Mô tả chi tiết")]
        public string? MoTaChiTiet { get; set; }

        [Required]
        [Display(Name = "Thời gian bắt đầu")]
        public DateTime ThoiGianBatDau { get; set; }

        [Required]
        [Display(Name = "Thời gian kết thúc")]
        public DateTime ThoiGianKetThuc { get; set; }

        [Required]
        [Display(Name = "Loại sự kiện")]
        public int LoaiId { get; set; }

        [Display(Name = "Số lượng tối đa")]
        public int? SoLuongToiDa { get; set; }

        [Display(Name = "Giá vé mặc định")]
        public decimal? GiaVeMacDinh { get; set; }

        // ====== THÔNG TIN ĐỊA ĐIỂM (SỰ KIỆN TRỰC TIẾP) ======
        [Display(Name = "Tên địa điểm")]
        public string? TenDiaDiem { get; set; }

        [Display(Name = "Địa chỉ")]
        public string? DiaChi { get; set; }

        [Display(Name = "Tổng số ghế tại địa điểm")]
        public int? SoChoNgoi { get; set; }

        [Display(Name = "Link bản đồ (Google Maps)")]
        public string? BanDo { get; set; }

        // ====== THÔNG TIN SỰ KIỆN TRỰC TUYẾN ======
        [Display(Name = "Nền tảng (Zoom, Google Meet...)")]
        public string? NenTang { get; set; }

        [Display(Name = "Link xem")]
        public string? LinkXem { get; set; }

        [Display(Name = "Mật khẩu phòng (nếu có)")]
        public string? MatKhauPhong { get; set; }

        [Display(Name = "Hướng dẫn tham gia")]
        public string? HuongDan { get; set; }

        // ====== CẤU HÌNH LOẠI VÉ (ĐƠN GIẢN, GIỐNG CÁC WEB VÉ) ======
        [Display(Name = "Tên loại vé 1")]
        public string? TenLoaiVe1 { get; set; }

        [Display(Name = "Giá vé 1")]
        public decimal? GiaVe1 { get; set; }

        [Display(Name = "Số lượng vé 1")]
        public int? SoLuongVe1 { get; set; }

        [Display(Name = "Tên loại vé 2")]
        public string? TenLoaiVe2 { get; set; }

        [Display(Name = "Giá vé 2")]
        public decimal? GiaVe2 { get; set; }

        [Display(Name = "Số lượng vé 2")]
        public int? SoLuongVe2 { get; set; }
        [Display(Name = "Số hàng ghế (A, B, C...)")]
        public int? SoHang { get; set; }

        [Display(Name = "Số ghế mỗi hàng")]
        public int? SoCotMoiHang { get; set; }

        [Display(Name = "Hàng VIP từ (1 = A, 2 = B...)")]
        public int? HangVipTu { get; set; }

        [Display(Name = "Hàng VIP đến")]
        public int? HangVipDen { get; set; }
        
        public string? SoDoJson { get; set; }


    }
}
