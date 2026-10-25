using System;
using System.ComponentModel.DataAnnotations;

namespace VeSuKienWeb.Models.KhachHang
{
    public class YeuCauHoanHuyVM
    {
        public int VeId { get; set; }
        public string MaVe { get; set; } = string.Empty;
        public string TenSuKien { get; set; } = string.Empty;
        public DateTime ThoiGianBatDau { get; set; }
        public decimal SoTien { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập lý do hủy/hoàn vé.")]
        [Display(Name = "Lý do hủy/hoàn vé")]
        public string LyDo { get; set; } = string.Empty;
        public string TenLoaiVe { get; set; } = string.Empty;
        public string? DanhSachGhe { get; set; }
        public int SoLuongVe { get; set; }
    }
}

