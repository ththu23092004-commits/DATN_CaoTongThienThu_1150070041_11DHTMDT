using System;

namespace VeSuKienWeb.Models
{
    public class LichSuXemSuKien
    {
        public int LichSuId { get; set; }

        public int NguoiDungId { get; set; }

        public int SuKienId { get; set; }

        public DateTime ThoiGianXem { get; set; } = DateTime.Now;

        public NguoiDung? NguoiDung { get; set; }

        public SuKien? SuKien { get; set; }
    }
}
