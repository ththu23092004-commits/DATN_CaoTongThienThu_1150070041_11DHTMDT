
using System.ComponentModel.DataAnnotations;

namespace VeSuKienWeb.Models.KhaoSatSoThich
{
    public class KhaoSatSoThichVM
    {
        [Required(ErrorMessage = "Vui lòng chọn ít nhất một chủ đề yêu thích.")]
        public List<int> ChuDeIds { get; set; } = new();

        public string? DiaDiemUuTien { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn hình thức tổ chức yêu thích.")]
        public string HinhThucUuTien { get; set; } = "KhongGioiHan";
    }
}
