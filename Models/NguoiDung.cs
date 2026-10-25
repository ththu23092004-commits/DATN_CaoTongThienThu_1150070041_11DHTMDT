using System;

namespace VeSuKienWeb.Models
{
    public class NguoiDung
    {
        public int NguoiDungId { get; set; }
        public string HoTen { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string MatKhau { get; set; } = string.Empty;
        public string? DienThoai { get; set; }
        public string? VaiTro { get; set; }   // "KhachHang", "ToChuc", "Admin"
        public DateTime NgayTao { get; set; } = DateTime.Now;
        public string TrangThai { get; set; } = "HoatDong";
    }
}

