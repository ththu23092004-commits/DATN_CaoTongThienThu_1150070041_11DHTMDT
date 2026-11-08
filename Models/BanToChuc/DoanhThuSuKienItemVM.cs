
using System;

namespace VeSuKienWeb.Models.BanToChuc
{
    public class DoanhThuSuKienItemVM
    {
        public int SuKienId { get; set; }

        public string TenSuKien { get; set; } = "";

        public DateTime ThoiGianBatDau { get; set; }

        public int SoVeDaBan { get; set; }

        public decimal TongThanhToan { get; set; }

        public decimal TongHoan { get; set; }

        public decimal DoanhThuThucNhan =>
            TongThanhToan - TongHoan;
    }
}

