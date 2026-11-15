
using System;

namespace VeSuKienWeb.Models.KhamPhaSuKien
{
    public class KhamPhaTinNhan
    {
        public long TinNhanId { get; set; }

        public long HoiThoaiId { get; set; }

        public string VaiTro { get; set; } = "user";

        public string NoiDung { get; set; } = string.Empty;

        public string? DuLieuKetQuaJson { get; set; }

        public DateTime NgayGui { get; set; } = DateTime.Now;

        public KhamPhaHoiThoai? HoiThoai { get; set; }
    }
}

