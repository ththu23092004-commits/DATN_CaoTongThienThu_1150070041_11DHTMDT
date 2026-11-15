
using System;
using System.Collections.Generic;
using VeSuKienWeb.Models;

namespace VeSuKienWeb.Models.KhamPhaSuKien
{
    public class KhamPhaHoiThoai
    {
        public long HoiThoaiId { get; set; }

        public int NguoiDungId { get; set; }

        public string TieuDe { get; set; } = "Cuộc trò chuyện mới";

        public string? NguCanhJson { get; set; }

        public string TrangThai { get; set; } = "DangMo";

        public DateTime NgayTao { get; set; } = DateTime.Now;

        public DateTime NgayCapNhat { get; set; } = DateTime.Now;

        public NguoiDung? NguoiDung { get; set; }

        public ICollection<KhamPhaTinNhan> TinNhans { get; set; }
            = new List<KhamPhaTinNhan>();
    }
}

