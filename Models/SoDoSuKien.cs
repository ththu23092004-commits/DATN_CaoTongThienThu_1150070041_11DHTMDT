
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VeSuKienWeb.Models
{
    public class SoDoSuKien
    {
        [Key]
        public int SoDoId { get; set; }

        public int SuKienId { get; set; }

        public int ChieuRong { get; set; } = 1000;

        public int ChieuCao { get; set; } = 700;

        [MaxLength(20)]
        public string TrangThai { get; set; } = "BanNhap";

        public DateTime NgayCapNhat { get; set; } = DateTime.UtcNow;

        public SuKien SuKien { get; set; } = null!;

        public ICollection<KhuVucChoNgoi> KhuVucChoNgois { get; set; }
            = new List<KhuVucChoNgoi>();

        public ICollection<ThanhPhanSoDo> ThanhPhanSoDos { get; set; }
            = new List<ThanhPhanSoDo>();
    }
}

