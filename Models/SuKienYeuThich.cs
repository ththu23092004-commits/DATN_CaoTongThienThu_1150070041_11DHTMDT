
using System;

namespace VeSuKienWeb.Models
{
    public class SuKienYeuThich
    {
        public int NguoiDungId { get; set; }
        public int SuKienId { get; set; }

        public DateTime NgayYeuThich { get; set; }
            = DateTime.Now;

        public NguoiDung? NguoiDung { get; set; }
        public SuKien? SuKien { get; set; }
    }
}

