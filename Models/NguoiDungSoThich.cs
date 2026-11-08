using System;

namespace VeSuKienWeb.Models
{
    public class NguoiDungSoThich
    {
        public int NguoiDungId { get; set; }

        public int ChuDeId { get; set; }

        public DateTime NgayChon { get; set; } = DateTime.Now;

        public NguoiDung? NguoiDung { get; set; }

        public ChuDeSuKien? ChuDeSuKien { get; set; }
    }
}
