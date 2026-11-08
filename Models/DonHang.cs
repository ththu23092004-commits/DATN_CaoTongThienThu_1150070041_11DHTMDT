using System;
using System.Collections.Generic;

namespace VeSuKienWeb.Models
{
    public class DonHang
    {
        
public decimal SoTienHoan { get; set; } = 0;

public DateTime? NgayHoan { get; set; }

public string? TrangThaiHoan { get; set; }

        public int DonHangId { get; set; }
        public int NguoiDungId { get; set; }
        public DateTime NgayDat { get; set; }
        public decimal TongTien { get; set; }
        public string TrangThai { get; set; } = "DangXuLy"; 
        // DangXuLy, ThanhCong, Huy

        // Navigation
        public NguoiDung NguoiDung { get; set; } = null!;
        public ICollection<Ve> Ves { get; set; } = new List<Ve>();
    }
}

