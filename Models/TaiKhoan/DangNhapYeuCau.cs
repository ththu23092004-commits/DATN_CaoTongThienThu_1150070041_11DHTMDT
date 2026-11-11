using System.ComponentModel.DataAnnotations;

namespace VeSuKienWeb.Models.TaiKhoan
{
    public class DangNhapYeuCau
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string MatKhau { get; set; } = string.Empty;
        public bool GhiNhoDangNhap { get; set; }
    }
}
