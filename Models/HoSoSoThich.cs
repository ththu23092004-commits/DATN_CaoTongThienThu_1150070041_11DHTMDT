using System;

namespace VeSuKienWeb.Models
{
    public class HoSoSoThich
    {
        public int HoSoId { get; set; }

        public int NguoiDungId { get; set; }

        public string? DiaDiemUuTien { get; set; }

        public string HinhThucUuTien { get; set; } = "KhongGioiHan";

        public bool DaHoanThanhKhaoSat { get; set; } = false;

        public DateTime NgayCapNhat { get; set; } = DateTime.Now;

        public NguoiDung? NguoiDung { get; set; }
    }
}
