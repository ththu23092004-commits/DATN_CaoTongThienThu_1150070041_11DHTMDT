using System.Collections.Generic;

namespace VeSuKienWeb.Models
{
    public class ChuDeSuKien
    {
        public int ChuDeId { get; set; }
        public string TenChuDe { get; set; } = string.Empty;
        public string? MoTa { get; set; }
        public bool TrangThai { get; set; } = true;
    }
}
